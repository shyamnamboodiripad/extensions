// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal partial class DefaultHybridCache
{
    private readonly ConcurrentDictionary<StampedeKey, StampedeState> _currentOperations = new();

    // returns true for a new session (in which case: we need to start the work), false for a pre-existing session
    public bool GetOrCreateStampedeState<TState, T>(string key, HybridCacheEntryFlags flags, out StampedeState<TState, T> stampedeState, bool canBeCanceled, IEnumerable<string>? tags)
    {
        var stampedeKey = new StampedeKey(key, flags);

        // Double-checked locking to try to avoid unnecessary sessions in race conditions,
        // while avoiding the lock completely whenever possible.
        if (TryJoinExistingSession(this, stampedeKey, out StampedeState<TState, T>? existing))
        {
            stampedeState = existing;
            return false; // someone ELSE is running the work
        }

        // Most common scenario here, then, is that we're not fighting with anyone else
        // go ahead and create a placeholder state object and *try* to add it.
        stampedeState = new StampedeState<TState, T>(this, stampedeKey, TagSet.Create(tags), canBeCanceled);
        if (_currentOperations.TryAdd(stampedeKey, stampedeState))
        {
            // successfully added; indeed, no-one else was fighting: we're done
            return true; // the CURRENT caller is responsible for making the work happen
        }

        // Hmmm, failed to add - there's concurrent activity on the same key; we're now
        // in very rare race condition territory; go ahead and take a lock while we
        // collect our thoughts.

        // see notes in SyncLock.cs
        lock (GetPartitionedSyncLock(in stampedeKey))
        {
            // check again while we hold the lock
            if (TryJoinExistingSession(this, stampedeKey, out existing))
            {
                // we found an existing state we can join; do that
                stampedeState.SetCanceled(); // to be thorough: mark our speculative one as doomed (no-one has seen it, though)
                stampedeState = existing; // and replace with the one we found
                return false; // someone ELSE is running the work

                // Note that in this case we allocated a StampedeState<TState, T> that got dropped on
                // the floor; in the grand scheme of things, that's OK; this is a rare outcome.
            }

            // Check whether the value was L1-cached by an outgoing operation (for *us* to check needs local-cache-read,
            // and for *them* to have updated needs local-cache-write, but since the shared us/them key includes flags,
            // we can skip this if *either* flag is set).
            if ((flags & HybridCacheEntryFlags.DisableLocalCache) == 0
                && TryGetExisting<T>(key, out CacheItem<T>? typed)
                && typed.TryReserve())
            {
                stampedeState.SetResultDirect(typed);
                return false; // the work has ALREADY been done
            }

            // Otherwise, either nothing existed - or the thing that already exists can't be joined
            // in that case, go ahead and use the state that we invented a moment ago (outside of the lock).
            _currentOperations[stampedeKey] = stampedeState;
            return true; // the CURRENT caller is responsible for making the work happen
        }

        static bool TryJoinExistingSession(DefaultHybridCache @this, in StampedeKey stampedeKey,
            [NotNullWhen(true)] out StampedeState<TState, T>? stampedeState)
        {
            if (@this._currentOperations.TryGetValue(stampedeKey, out StampedeState? found))
            {
                if (found is not StampedeState<TState, T> tmp)
                {
                    ThrowWrongType(stampedeKey.Key, found.Type, typeof(T));
                }

                if (tmp.TryAddCaller())
                {
                    // we joined an existing session
                    stampedeState = tmp;
                    return true;
                }
            }

            stampedeState = null;
            return false;
        }

        [DoesNotReturn]
        static void ThrowWrongType(string key, Type existingType, Type newType)
        {
            Debug.Assert(existingType != newType, "should be different types");
            throw new InvalidOperationException(
                $"All calls to {nameof(HybridCache)} with the same key should use the same data type; the same key is being used for '{existingType.FullName}' and '{newType.FullName}' data")
            {
                Data = { { "CacheKey", key } }
            };
        }
    }

    internal int DebugGetCallerCount(string key, HybridCacheEntryFlags? flags = null)
    {
        var stampedeKey = new StampedeKey(key, flags ?? _defaultFlags);
        return _currentOperations.TryGetValue(stampedeKey, out StampedeState? state) ? state.DebugCallerCount : 0;
    }
}
