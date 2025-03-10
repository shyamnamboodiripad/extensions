// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.Inference;
using Azure.Core;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal sealed class ContentSafetyService(
    ContentSafetyServiceConfiguration serviceConfiguration,
    string evaluatorName,
    string metricName,
    IHttpClientFactory httpClientFactory)
{
    public ValueTask<NumericMetric> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatMessage modelResponse,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        messages = [.. messages, modelResponse];

        return EvaluateAsync(
            messages.ToAzureAIInferenceChatRequestMessages(),
            additionalContext,
            cancellationToken);
    }

    public async ValueTask<NumericMetric> EvaluateAsync(
        IEnumerable<ChatRequestMessage> messages,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        string serviceUrl = await GetServiceUrlAsync(cancellationToken).ConfigureAwait(false);
        string annotationTask = GetAnnotationTask(metricName);
        await EnsureServiceAvailabilityAsync(serviceUrl, annotationTask, cancellationToken).ConfigureAwait(false);
    }

    private static string GetAnnotationTask(string metricName)
    {
        return metricName switch
        {
            "hate_fairness" => "content harm",
            "hate_unfairness" => "content harm",
            "self_harm" => "content harm",
            "sexual" => "content harm",
            "violence" => "content harm",
            "protected_material" => "protected material",
            "generic_groundedness" => "groundedness",
            "eci" => "eci",
            "xpia" => "xpia",
        };
    }

    private async ValueTask EnsureServiceAvailabilityAsync(
        string serviceUrl,
        string capability,
        CancellationToken cancellationToken)
    {
        string serviceAvailabilityUrl = $"{serviceUrl}/checkannotation";

        HttpResponseMessage response =
            await GetResponseAsync(serviceAvailabilityUrl, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"""
                The Azure AI Content Safety service is either unavailable in this region, or you lack the necessary permissions to access the AI project (status code: {response.StatusCode}).
                To troubleshoot, see https://aka.ms/azsdk/python/evaluation/safetyevaluator/troubleshoot.
                """);
        }

        string responseContent =
#if NET
            await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        using JsonDocument document = JsonDocument.Parse(responseContent);
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            string? supportedCapability = element.GetString();
            if (!string.IsNullOrWhiteSpace(supportedCapability) &&
                string.Equals(supportedCapability, capability, StringComparison.Ordinal))
            {
                return;
            }
        }

        throw new InvalidOperationException(
            $"""
            The required {nameof(capability)} '{capability}' is not supported by the Azure AI Content Safety service in this region.
            To troubleshoot, see https://aka.ms/azsdk/python/evaluation/safetyevaluator/troubleshoot.
            """);
    }

    private async ValueTask<string> GetServiceUrlAsync(CancellationToken cancellationToken)
    {
        string discoveryUrl = await GetServiceDiscoveryUrlAsync(cancellationToken).ConfigureAwait(false);

        string serviceUrl =
            $"{discoveryUrl}/raisvc/v1.0" +
            $"/subscriptions/{serviceConfiguration.SubscriptionId}" +
            $"/resourceGroups/{serviceConfiguration.ResourceGroupName}" +
            $"/providers/Microsoft.MachineLearningServices/workspaces/{serviceConfiguration.ProjectName}";

        return serviceUrl;
    }

    private async ValueTask<string> GetServiceDiscoveryUrlAsync(CancellationToken cancellationToken)
    {
        string requestUrl =
            $"https://management.azure.com/subscriptions/{serviceConfiguration.SubscriptionId}" +
            $"/resourceGroups/{serviceConfiguration.ResourceGroupName}" +
            $"/providers/Microsoft.MachineLearningServices/workspaces/{serviceConfiguration.ProjectName}" +
            $"?api-version=2023-08-01-preview";

        HttpResponseMessage response =
            await GetResponseAsync(requestUrl, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"""
                Failed to retrieve discovery URL for Azure AI Content Safety service (status code: {response.StatusCode}).
                To troubleshoot, see https://aka.ms/azsdk/python/evaluation/safetyevaluator/troubleshoot.
                """);
        }

        string responseContent =
#if NET
            await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        using JsonDocument document = JsonDocument.Parse(responseContent);
        string? discoveryUrl = document.RootElement.GetProperty("properties").GetProperty("discoveryUrl").GetString();
        if (string.IsNullOrWhiteSpace(discoveryUrl))
        {
            throw new InvalidOperationException("Failed to retrieve discovery URL for Azure AI Content Safety service. See https://aka.ms/azsdk/python/evaluation/safetyevaluator/troubleshoot.");
        }

        Uri discoveryUri = new Uri(discoveryUrl);
        return $"{discoveryUri.Scheme}://{discoveryUri.Host}";
    }

    private async ValueTask<HttpResponseMessage> GetResponseAsync(
        string requestUrl,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Content = new StringContent(payload ?? string.Empty);
        await AddHeadersAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);

#pragma warning disable CA2000 // Dispose objects before losing scope
        // See https://learn.microsoft.com/en-us/dotnet/api/system.net.http.ihttpclientfactory.createclient
        // It is generally not necessary to dispose of the HttpClient as the IHttpClientFactory tracks and disposes
        // resources used by the HttpClient.
        HttpClient client = httpClientFactory.CreateClient();
#pragma warning restore CA2000

        HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response;
    }

    private async ValueTask AddHeadersAsync(
        HttpRequestMessage httpRequestMessage,
        CancellationToken cancellationToken = default)
    {
        // TASK: Replace 'unknown' with appropriate version below.
        string userAgent =
            $"microsoft-extensions-ai-evaluation/{Constants.Version} (type=evaluator; subtype={evaluatorName})";

        AccessToken token =
            await serviceConfiguration.Credential.GetTokenAsync(
                new TokenRequestContext(scopes: ["https://management.azure.com/.default"]),
                cancellationToken).ConfigureAwait(false);

        httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        httpRequestMessage.Headers.UserAgent.ParseAdd(userAgent);
    }
}
