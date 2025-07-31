// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI.Evaluation.Console.Utilities.Telemetry;

internal static class EnvironmentHelper
{
    internal static bool GetEnvironmentVariableAsBool(string name) =>
        Environment.GetEnvironmentVariable(name)?.ToUpperInvariant() switch
        {
            "TRUE" => true,
            "1" => true,
            "YES" => true,
            _ => false
        };
}
