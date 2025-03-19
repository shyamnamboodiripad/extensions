// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal static class ContentSafetyMetricUtilities
{
    internal static string GetMetricName(this ContentSafetyMetric metric) =>
        metric switch
        {
            ContentSafetyMetric.HateAndUnfairness => "hate_fairness",
            ContentSafetyMetric.SelfHarm => "self_harm",
            ContentSafetyMetric.Violence => "violence",
            ContentSafetyMetric.InappropriateMaterial => "sexual",
            ContentSafetyMetric.ProtectedMaterial => "protected_material",
            ContentSafetyMetric.Groundedness => "generic_groundedness",
            ContentSafetyMetric.InferenceSensitiveAttributes => "ungrounded_attributes",
            ContentSafetyMetric.CodeVulnerability => "code_vulnerability",
            ContentSafetyMetric.XPIA => "xpia",
            ContentSafetyMetric.ECI => "eci",
            _ => throw new NotSupportedException($"{nameof(ContentSafetyMetric)} with value '{metric}' is not supported.")
        };

    internal static string GetAnnotationTask(string contentSafetyMetricName) =>
        contentSafetyMetricName switch
        {
            "hate_fairness" => "content harm",
            "hate_unfairness" => "content harm",
            "self_harm" => "content harm",
            "sexual" => "content harm",
            "violence" => "content harm",
            "protected_material" => "protected material",
            "generic_groundedness" => "groundedness",
            "ungrounded_attributes" => "inference sensitive attributes",
            "code_vulnerability" => "code vulnerability",
            "xpia" => "xpia",
            "eci" => "eci",
            _ => throw new NotSupportedException($"The metric '{contentSafetyMetricName}' is not supported by the Azure AI Content Safety service.")
        };

    internal static string GetDisplayName(string contentSafetyMetricName) =>
        contentSafetyMetricName switch
        {
            "hate_unfairness" => ContentSafetyMetric.HateAndUnfairness.GetDisplayName(),
            "hate_fairness" => ContentSafetyMetric.HateAndUnfairness.GetDisplayName(),
            "self_harm" => ContentSafetyMetric.SelfHarm.GetDisplayName(),
            "violence" => ContentSafetyMetric.Violence.GetDisplayName(),
            "sexual" => ContentSafetyMetric.InappropriateMaterial.GetDisplayName(),
            "protected_material" => ContentSafetyMetric.ProtectedMaterial.GetDisplayName(),
            "generic_groundedness" => ContentSafetyMetric.Groundedness.GetDisplayName(),
            "ungrounded_attributes" => ContentSafetyMetric.InferenceSensitiveAttributes.GetDisplayName(),
            "code_vulnerability" => ContentSafetyMetric.CodeVulnerability.GetDisplayName(),
            "xpia" => ContentSafetyMetric.XPIA.GetDisplayName(),
            "eci" => ContentSafetyMetric.ECI.GetDisplayName(),
            _ => throw new NotSupportedException($"Content safety metric with name '{contentSafetyMetricName}' is not supported.")
        };

    internal static string GetDisplayName(this ContentSafetyMetric metric) =>
        metric switch
        {
            ContentSafetyMetric.HateAndUnfairness => "Hate And Unfairness",
            ContentSafetyMetric.SelfHarm => "Self Harm",
            ContentSafetyMetric.Violence => "Violence",
            ContentSafetyMetric.InappropriateMaterial => "Inappropriate Material",
            ContentSafetyMetric.ProtectedMaterial => "Protected Material",
            ContentSafetyMetric.Groundedness => "Groundedness Pro",
            ContentSafetyMetric.InferenceSensitiveAttributes => "Inference Sensitive Attributes",
            ContentSafetyMetric.CodeVulnerability => "Code Vulnerability",
            ContentSafetyMetric.XPIA => "XPIA",
            ContentSafetyMetric.ECI => "ECI",
            _ => throw new NotSupportedException($"{nameof(ContentSafetyMetric)} with value '{metric}' is not supported.")
        };
}
