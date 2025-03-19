// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// An enumeration that identifies the metrics that the <see cref="ContentSafetyEvaluator"/> can evaluate.
/// </summary>
public enum ContentSafetyMetric
{
    /// <summary>
    /// A place holder that indicates the absence of any content safety metrics.
    /// </summary>
    None,

    /// <summary>
    /// The content safety metric for hate and unfairness.
    /// </summary>
    HateAndUnfairness,

    /// <summary>
    /// The content safety metric for self-harm.
    /// </summary>
    SelfHarm,

    /// <summary>
    /// The content safety metric for violence.
    /// </summary>
    Violence,

    /// <summary>
    /// The content safety metric for sexual content.
    /// </summary>
    InappropriateMaterial,

    /// <summary>
    /// The content safety metric for protected material.
    /// </summary>
    ProtectedMaterial,

    /// <summary>
    /// The content safety metric for groundedness.
    /// </summary> 
    Groundedness,

    /// <summary>
    /// The content safety metric for inference sensitive attributes.
    /// </summary>
    InferenceSensitiveAttributes,

    /// <summary>
    /// The content safety metric for code vulnerability.
    /// </summary>
    CodeVulnerability,

    /// <summary>
    /// The content safety metric for XPIA.
    /// </summary>
    XPIA,

    /// <summary>
    /// The content safety metric for ECI.
    /// </summary>
    ECI,
}
