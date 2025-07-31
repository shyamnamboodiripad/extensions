// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Utilities;

namespace Microsoft.Extensions.AI.Evaluation.Console.Utilities.Telemetry;

internal static class TelemetryExtensions
{
    internal static string ToTelemetryPropertyValue(this bool value) =>
        value ? TelemetryConstants.PropertyValues.True : TelemetryConstants.PropertyValues.False;

    internal static void TrackOperation(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Action operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            TimeSpan duration = TimingHelper.ExecuteWithTiming(operation);
            telemetryHelper.TrackOperationSuccess(operationName, duration, properties, metrics);
        }
        catch (Exception ex)
        {
            telemetryHelper.TrackOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

    internal static TResult TrackOperation<TResult>(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Func<TResult> operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            (TResult result, TimeSpan duration) = TimingHelper.ExecuteWithTiming(operation);
            telemetryHelper.TrackOperationSuccess(operationName, duration, properties, metrics);
            return result;
        }
        catch (Exception ex)
        {
            telemetryHelper.TrackOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

#pragma warning disable EA0014 // The async method doesn't support cancellation
    internal static async Task TrackOperationAsync<TResult>(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Func<Task> operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            TimeSpan duration = await TimingHelper.ExecuteWithTimingAsync(operation).ConfigureAwait(false);
            telemetryHelper.TrackOperationSuccess(operationName, duration, properties, metrics);
        }
        catch (Exception ex)
        {
            telemetryHelper.TrackOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

    internal static async ValueTask TrackOperationAsync<TResult>(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Func<ValueTask> operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            TimeSpan duration = await TimingHelper.ExecuteWithTimingAsync(operation).ConfigureAwait(false);
            telemetryHelper.TrackOperationSuccess(operationName, duration, properties, metrics);
        }
        catch (Exception ex)
        {
            telemetryHelper.TrackOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

    internal static async Task<TResult> TrackOperationAsync<TResult>(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Func<Task<TResult>> operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            (TResult result, TimeSpan duration) =
                await TimingHelper.ExecuteWithTimingAsync(operation).ConfigureAwait(false);

            telemetryHelper.TrackOperationSuccess(operationName, duration, properties, metrics);
            return result;
        }
        catch (Exception ex)
        {
            telemetryHelper.TrackOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

    internal static async ValueTask<TResult> TrackOperationAsync<TResult>(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Func<ValueTask<TResult>> operation,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        try
        {
            (TResult result, TimeSpan duration) =
                await TimingHelper.ExecuteWithTimingAsync(operation).ConfigureAwait(false);

            telemetryHelper.TrackOperationSuccess(operationName, duration, properties, metrics);
            return result;
        }
        catch (Exception ex)
        {
            telemetryHelper.TrackOperationFailure(operationName, ex, properties, metrics);
            throw;
        }
    }

    private static void TrackOperationSuccess(
        this TelemetryHelper telemetryHelper,
        string operationName,
        TimeSpan duration,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        properties ??= new Dictionary<string, string>();
        properties.Add(TelemetryConstants.PropertyNames.Success, TelemetryConstants.PropertyValues.True);

        metrics ??= new Dictionary<string, double>();
        metrics.Add(TelemetryConstants.PropertyNames.DurationInMilliseconds, duration.TotalMilliseconds);

        telemetryHelper.TrackEvent(eventName: operationName, properties, metrics);
    }

    private static void TrackOperationFailure(
        this TelemetryHelper telemetryHelper,
        string operationName,
        Exception exception,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null)
    {
        properties ??= new Dictionary<string, string>();
        properties.Add(TelemetryConstants.PropertyNames.Success, TelemetryConstants.PropertyValues.False);

        metrics ??= new Dictionary<string, double>();
        metrics.Add(TelemetryConstants.PropertyNames.DurationInMilliseconds, 0);

        telemetryHelper.TrackEvent(eventName: operationName, properties, metrics);
        telemetryHelper.TrackException(exception, properties, metrics);
    }
#pragma warning restore EA0014
}
