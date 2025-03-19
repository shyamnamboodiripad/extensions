// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Extension methods for <see cref="EvaluationMetric"/>.
/// </summary>
public static class EvaluationMetricExtensions
{
    /// <summary>
    /// Returns <see langword="true"/> if the supplied <paramref name="metric"/> contains any
    /// <see cref="EvaluationDiagnostic"/> matching the supplied <paramref name="predicate"/>; <see langword="false"/>
    /// otherwise.
    /// </summary>
    /// <param name="metric">The <see cref="EvaluationMetric"/> that is to be inspected.</param>
    /// <param name="predicate">
    /// A predicate that returns <see langword="true"/> if a matching <see cref="EvaluationDiagnostic"/> is found;
    /// <see langword="false"/> otherwise.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the supplied <paramref name="metric"/> contains any
    /// <see cref="EvaluationDiagnostic"/> matching the supplied <paramref name="predicate"/>; <see langword="false"/>
    /// otherwise.
    /// </returns>
    public static bool ContainsDiagnostics(
        this EvaluationMetric metric,
        Func<EvaluationDiagnostic, bool>? predicate = null)
    {
        _ = Throw.IfNull(metric);

        return predicate is null ? metric.Diagnostics.Any() : metric.Diagnostics.Any(predicate);
    }

    /// <summary>
    /// Adds the supplied <see cref="EvaluationDiagnostic"/> to the supplied <see cref="EvaluationMetric"/>'s
    /// <see cref="EvaluationMetric.Diagnostics"/> collection.
    /// </summary>
    /// <param name="metric">The <see cref="EvaluationMetric"/>.</param>
    /// <param name="diagnostic">The <see cref="EvaluationDiagnostic"/> to be added.</param>
    public static void AddDiagnostic(this EvaluationMetric metric, EvaluationDiagnostic diagnostic)
    {
        _ = Throw.IfNull(metric);

        metric.Diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Adds the supplied <see cref="EvaluationDiagnostic"/>s to the supplied <see cref="EvaluationMetric"/>'s
    /// <see cref="EvaluationMetric.Diagnostics"/> collection.
    /// </summary>
    /// <param name="metric">The <see cref="EvaluationMetric"/>.</param>
    /// <param name="diagnostics">The <see cref="EvaluationDiagnostic"/>s to be added.</param>
    public static void AddDiagnostics(this EvaluationMetric metric, IEnumerable<EvaluationDiagnostic> diagnostics)
    {
        _ = Throw.IfNull(metric);
        _ = Throw.IfNull(diagnostics);

        foreach (EvaluationDiagnostic diagnostic in diagnostics)
        {
            metric.AddDiagnostic(diagnostic);
        }
    }
}
