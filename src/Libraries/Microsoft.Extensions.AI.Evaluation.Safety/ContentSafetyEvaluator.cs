// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// An <see langword="abstract"/> base class that can be used to implement <see cref="IEvaluator"/>s that utilize the
/// Azure AI Content Safety service to produce <see cref="EvaluationResult"/>s containing <see cref="NumericMetric"/>
/// scores for content safety metrics such as hate and unfairness, self-harm, violence etc.
/// </summary>
/// <param name="serviceConfiguration">
/// Specifies the Azure AI project that should be used and credentials that should be used when this
/// <see cref="ContentSafetyEvaluator"/> communicates with the Azure AI Content Safety service to perform evaluations.
/// </param>
/// <param name="httpClientFactory">
/// The <see cref="IHttpClientFactory"/> that should be used to create the <see cref="HttpClient"/> that this
/// <see cref="ContentSafetyEvaluator"/> uses when communicating with the Azure AI Content Safety service.
/// </param>
public abstract class ContentSafetyEvaluator(
    ContentSafetyServiceConfiguration serviceConfiguration,
    IHttpClientFactory httpClientFactory) : IEvaluator
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    /// <summary>
    /// Gets the Azure AI project that should be used and credentials that should be used when this
    /// <see cref="ContentSafetyEvaluator"/> communicates with the Azure AI Content Safety service to perform
    /// evaluations.
    /// </summary>
    protected ContentSafetyServiceConfiguration ServiceConfiguration { get; } = serviceConfiguration;

    /// <summary>
    /// Gets the <see cref="IHttpClientFactory"/> that should be used to create the <see cref="HttpClient"/> that this
    /// <see cref="ContentSafetyEvaluator"/> uses when communicating with the Azure AI Content Safety service.
    /// </summary>
    protected IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;

    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> produced by this
    /// <see cref="IEvaluator"/>.
    /// </summary>
    protected abstract string MetricName { get; }

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatMessage modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {

    }
}
