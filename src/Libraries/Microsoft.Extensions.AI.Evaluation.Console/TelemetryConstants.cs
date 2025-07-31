// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.AI.Evaluation.Console.Utilities.Telemetry;

namespace Microsoft.Extensions.AI.Evaluation.Console;

internal static class TelemetryConstants
{
    internal static class PropertyNames
    {
        internal const string DevDeviceId = "DevDeviceId";
        internal const string OSVersion = "OSVersion";
        internal const string OSPlatform = "OSPlatform";
        internal const string KernelVersion = "KernelVersion";
        internal const string RuntimeId = "RuntimeId";
        internal const string ProductVersion = "ProductVersion";

        internal const string Success = "success";
        internal const string DurationInMilliseconds = "DurationInMilliseconds";

        internal const string StorageType = "storageType";
        internal const string LastN = "lastN";
        internal const string Format = "format";
        internal const string OpenReport = "openReport";
    }

    internal static class PropertyValues
    {
        internal const string True = "true";
        internal const string False = "false";
        internal const string StorageTypeDisk = "disk";
        internal const string StorageTypeAzure = "azure";
    }

#pragma warning disable S103 // Lines should not be too long
    internal const string TelemetryOptOutMessage =
        $"""
        Telemetry
        ---------
        The .NET tools collect usage data in order to help us improve your experience. The data is anonymous and doesn't include command-line arguments. The data is collected by Microsoft, and you can opt-out of this data collection by setting the {TelemetryConstants.TelemetryOptOutEnvironmentVariableName} environment variable to '1' or 'true' using your favorite shell.
        """;
#pragma warning restore S103

    internal const string TelemetryOptOutEnvironmentVariableName = "DOTNET_AIEVAL_TELEMETRY_OPTOUT";
    private const string SkipFirstTimeExperienceEnvironmentVariableName = "DOTNET_AIEVAL_SKIP_FIRST_TIME_EXPERIENCE";

    internal static bool IsTelemetryEnabled { get; } =
        !EnvironmentHelper.GetEnvironmentVariableAsBool(TelemetryOptOutEnvironmentVariableName) &&
        (!IsFirstTimeExperienceEnabled || File.Exists(FirstUseSentinelFilePath));

    internal static bool IsFirstTimeExperienceEnabled { get; } =
        !EnvironmentHelper.GetEnvironmentVariableAsBool(SkipFirstTimeExperienceEnvironmentVariableName);

    internal static string? FirstUseSentinelFilePath { get; } = GetFirstUseSentinelFilePath();

    private static string? GetFirstUseSentinelFilePath()
    {
        string? dotnetProfileDirectoryPath = GetDotnetProfileDirectoryPath();
        if (string.IsNullOrWhiteSpace(dotnetProfileDirectoryPath))
        {
            return null;
        }

        return Path.Combine(dotnetProfileDirectoryPath, $"{Constants.Version}.dotnetAIEvalFirstUseSentinel");

        static string? GetDotnetProfileDirectoryPath()
        {
            string? dotnetProfileDirectoryPath = Environment.GetEnvironmentVariable("DOTNET_HOME");

            if (string.IsNullOrWhiteSpace(dotnetProfileDirectoryPath))
            {
                dotnetProfileDirectoryPath =
                    Environment.GetEnvironmentVariable(
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? "USERPROFILE"
                            : "HOME");
            }

            return string.IsNullOrWhiteSpace(dotnetProfileDirectoryPath)
                ? null
                : Path.Combine(dotnetProfileDirectoryPath, ".dotnet");
        }
    }
}
