// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal sealed class ContentSafetyService(
    ContentSafetyServiceConfiguration serviceConfiguration,
    string evaluatorName,
    string metricName,
    IHttpClientFactory httpClientFactory)
{
    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        CancellationToken cancellationToken = default)
    {
        string serviceUrl = await GetServiceUrlAsync(cancellationToken).ConfigureAwait(false);
        string annotationTask = ContentSafetyMetricUtilities.GetAnnotationTask(metricName);
        await EnsureServiceAvailabilityAsync(serviceUrl, annotationTask, cancellationToken).ConfigureAwait(false);

        string contentType;
        IEnumerable<ChatMessage> conversation;
        IEnumerable<EvaluationDiagnostic>? diagnostics;
        (contentType, conversation, diagnostics) = PreProcessMessages(messages, modelResponse);

        JsonObject payload = GeneratePayload(contentType, conversation, annotationTask);
        string resultUrl = await SubmitAnnotationRequestAsync(serviceUrl, payload, cancellationToken).ConfigureAwait(false);
        string annotationResult = await FetchAnnotationResultAsync(resultUrl, cancellationToken).ConfigureAwait(false);
        EvaluationResult result = ParseAnnotationResult(annotationResult);
        return result;
    }

    private static EvaluationResult ParseAnnotationResult(string annotationResponse)
    {
#pragma warning disable S125 // Sections of code should not be commented out
        // Example annotation response:
        // [
        //   {
        //     "violence": "{\"label\":0,\"reasoning\":\"The system's response provides factual information and does not contain any violent content.\",\"version\":\"0.4\"}",
        //     "self_harm": "{\"label\":0,\"reasoning\":\"The system's response provides factual information and does not contain any self-harm-related content.\",\"version\":\"0.4\"}",
        //     "hate_fairness": "{\"label\":0,\"reasoning\":\"The system's response contains no negative sentiment or hateful and unfair content present.\",\"version\":\"0.4\"}",
        //   }
        // ]
#pragma warning restore S125

        List<EvaluationMetric> metrics = [];

        using JsonDocument annotationResponseDocument = JsonDocument.Parse(annotationResponse);
        foreach (JsonElement metricElement in annotationResponseDocument.RootElement.EnumerateArray())
        {
            foreach (JsonProperty property in metricElement.EnumerateObject())
            {
                string metricDisplayName = ContentSafetyMetricUtilities.GetDisplayName(property.Name);
                string metricDetails = property.Value.GetString()!;

                using JsonDocument metricDetailsDocument = JsonDocument.Parse(metricDetails);
                JsonElement metricDetailsRootElement = metricDetailsDocument.RootElement;

                JsonElement labelElement = metricDetailsRootElement.GetProperty("label");
                string? reason = metricDetailsRootElement.GetProperty("reasoning").GetString();

                switch (labelElement.ValueKind)
                {
                    case JsonValueKind.Number:
                        double doubleValue = labelElement.GetDouble();
                        NumericMetric numericMetric = new NumericMetric(metricDisplayName, doubleValue, reason);
                        numericMetric.Interpretation = numericMetric.InterpretScore();
                        metrics.Add(numericMetric);
                        break;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        bool booleanValue = labelElement.GetBoolean();
                        BooleanMetric booleanMetric = new BooleanMetric(metricDisplayName, booleanValue, reason);
                        booleanMetric.Interpretation = booleanMetric.InterpretScore(goodValue: metricDisplayName == "Groundedness Pro");
                        metrics.Add(booleanMetric);
                        break;

                    case JsonValueKind.String:
                        string stringValue = labelElement.GetString()!;
                        StringMetric stringMetric = new StringMetric(metricDisplayName, stringValue, reason);
                        metrics.Add(stringMetric);
                        break;

                    default:
                        stringMetric = new StringMetric(metricDisplayName, labelElement.ToString(), reason);
                        metrics.Add(stringMetric);
                        break;
                }
            }
        }

        return new EvaluationResult(metrics);
    }

    private (string contentType, IEnumerable<ChatMessage> conversation, IEnumerable<EvaluationDiagnostic>? diagnostics)
        PreProcessMessages(IEnumerable<ChatMessage> messages, ChatResponse modelResponse)
    {
        int ignoredMessageCount = 0;

        int userMessageCount = 0;
        ChatMessage? lastUserMessage = null;
        foreach (ChatMessage message in messages)
        {
            if (message.Role == ChatRole.User)
            {
                ++userMessageCount;
                lastUserMessage = message;
            }
            else
            {
                ++ignoredMessageCount;
            }
        }

        if (lastUserMessage is null)
        {
            throw new InvalidOperationException(
                $"{evaluatorName} requires at least one message with role '{ChatRole.User}' to be present in the supplied the conversation history.");
        }

        int assistantMessageCount = 0;
        ChatMessage? lastAssistantMessage = null;
        foreach (ChatMessage message in modelResponse.Messages)
        {
            if (message.Role == ChatRole.Assistant)
            {
                ++assistantMessageCount;
                lastAssistantMessage = message;
            }
            else
            {
                ++ignoredMessageCount;
            }
        }

        if (lastAssistantMessage is null)
        {
            throw new InvalidOperationException(
                $"{evaluatorName} requires at least one message with role '{ChatRole.Assistant}' to be present in the supplied model response.");
        }

        bool imageFound = false;
        bool unsupportedContentFound = false;
        foreach (AIContent content in lastAssistantMessage.Contents)
        {
            if (content is UriContent uriContent)
            {
                if (uriContent.HasTopLevelMediaType("image"))
                {
                    imageFound = true;
                }
                else
                {
                    unsupportedContentFound = true;
                }
            }
            else if (content is DataContent dataContent)
            {
                if (dataContent.HasTopLevelMediaType("image"))
                {
                    imageFound = true;
                }
                else
                {
                    unsupportedContentFound = true;
                }
            }
            else if (content is not TextContent)
            {
                unsupportedContentFound = true;
            }
        }

        string contentType = imageFound ? "image" : "text";
        IEnumerable<ChatMessage> conversation = [lastUserMessage, lastAssistantMessage];
        List<EvaluationDiagnostic>? diagnostics = null;

        if (userMessageCount > 1)
        {
            diagnostics =
            [
                EvaluationDiagnostic.Warning(
                    $"""
                    The supplied conversation history contains {userMessageCount} messages with role '{ChatRole.User}'.
                    {evaluatorName} does not support evaluating conversation with multiple turns.
                    The first {userMessageCount - 1} messages with role '{ChatRole.User}' in the supplied conversation history will be ignored.
                    """),
            ];
        }

        if (assistantMessageCount > 1)
        {
            diagnostics ??= new List<EvaluationDiagnostic>();
            diagnostics.Add(
                EvaluationDiagnostic.Warning(
                    $"""
                    The supplied model response contains {assistantMessageCount} messages with role '{ChatRole.Assistant}'.
                    {evaluatorName} does not support evaluating conversation with multiple turns.
                    The first {assistantMessageCount - 1} messages with role '{ChatRole.Assistant}' in the supplied model response will be ignored.
                    """));
        }

        if (ignoredMessageCount > 1)
        {
            diagnostics ??= new List<EvaluationDiagnostic>();
            diagnostics.Add(
                EvaluationDiagnostic.Warning(
                    $"""
                    The supplied conversation contains {ignoredMessageCount} messages with unsupported roles.
                    {evaluatorName} will only consider the last message with role '{ChatRole.User}' in the supplied conversation history,
                    and the last message with role '{ChatRole.Assistant}' in the supplied model response.
                    The unsupported messages will be ignored.
                    """));
        }

        if (unsupportedContentFound)
        {
            diagnostics ??= new List<EvaluationDiagnostic>();
            diagnostics.Add(
                EvaluationDiagnostic.Warning(
                    $"""
                    The supplied model response contains content of unsupported type.
                    {evaluatorName} only supports content of type '{nameof(TextContent)}', '{nameof(UriContent)}' and '{nameof(DataContent)}'.
                    For '{nameof(UriContent)}' and '{nameof(DataContent)}', only content with media type 'image/*' is supported.
                    The unsupported content will be ignored.
                    """));
        }

        return (contentType, conversation, diagnostics);
    }

    private JsonObject GeneratePayload(
        string contentType,
        IEnumerable<ChatMessage> conversation,
        string annotationTask)
    {
        JsonObject[] messages = GetMessages(conversation).ToArray();
        JsonObject payload = new JsonObject
        {
            ["ContentType"] = contentType,
            ["Contents"] =
                new JsonArray(
                    [
                        new JsonObject
                        {
                            ["messages"] = new JsonArray(messages)
                        }
                    ]),
            ["AnnotationTask"] = annotationTask
        };

        if (metricName != "protected_material")
        {
            payload["MetricList"] = new JsonArray([metricName]);
        }

        return payload;

        static IEnumerable<JsonObject> GetMessages(IEnumerable<ChatMessage> conversation)
        {
            foreach (ChatMessage message in conversation)
            {
                JsonObject[] contents = GetContents(message).ToArray();
                yield return new JsonObject
                {
                    ["role"] = message.Role.Value,
                    ["content"] = new JsonArray(contents)
                };
            }

            static IEnumerable<JsonObject> GetContents(ChatMessage message)
            {
                foreach (AIContent content in message.Contents)
                {
                    if (content is TextContent textContent)
                    {
                        yield return new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = textContent.Text
                        };
                    }
                    else if (content is UriContent uriContent && uriContent.HasTopLevelMediaType("image"))
                    {
                        yield return new JsonObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JsonObject
                            {
                                ["url"] = uriContent.Uri.AbsoluteUri
                            }
                        };
                    }
                    else if (content is DataContent dataContent && dataContent.HasTopLevelMediaType("image"))
                    {
                        var imageBytes = BinaryData.FromBytes(dataContent.Data);
                        var base64ImageData = Convert.ToBase64String(imageBytes.ToArray());
                        yield return new JsonObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JsonObject
                            {
                                ["url"] = $"data:{dataContent.MediaType};base64,{base64ImageData}"
                            }
                        };
                    }
                }
            }
        }
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

    private async ValueTask<string> SubmitAnnotationRequestAsync(
        string serviceUrl,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        string annotationUrl = $"{serviceUrl}/submitannotation";
        string payloadString = payload.ToJsonString();

        HttpResponseMessage response =
            await GetResponseAsync(annotationUrl, requestMethod: HttpMethod.Post, payloadString, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Failed to submit annotation request to the Azure AI Content Safety service (status code: {response.StatusCode}).");
        }

        string responseContent =
#if NET
            await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        using JsonDocument document = JsonDocument.Parse(responseContent);
        string? resultUrl = document.RootElement.GetProperty("location").GetString();

        if (string.IsNullOrWhiteSpace(resultUrl))
        {
            throw new InvalidOperationException(
                $"""
                Failed to retrieve the result location from the response for the annotation request submitted to The Azure AI Content Safety service.
                {responseContent}
                """);
        }

        return resultUrl!;
    }

    private async ValueTask<string> FetchAnnotationResultAsync(
        string resultUrl,
        CancellationToken cancellationToken)
    {
        // Task: Improve this code.
        int attempts = 0;
        const int MaximumAttempts = 5;
        const int TimeoutMilliseconds = 500;
        HttpResponseMessage response;
        do
        {
            response = await GetResponseAsync(resultUrl, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (++attempts > MaximumAttempts)
                {
                    throw new InvalidOperationException(
                        $"Failed to retrieve annotation result from the Azure AI Content Safety service (status code: {response.StatusCode}).");
                }
                else
                {
#pragma warning disable EA0002 // Use 'System.TimeProvider' to make the code easier to test
                    await Task.Delay(TimeoutMilliseconds * attempts, cancellationToken).ConfigureAwait(false);
#pragma warning restore EA0002
                }
            }
        }
        while (response.StatusCode != HttpStatusCode.OK);

        string responseContent =
#if NET
            await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        return responseContent;
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
        HttpMethod? requestMethod = null,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        requestMethod = requestMethod ?? HttpMethod.Get;
        using var request = new HttpRequestMessage(requestMethod, requestUrl);
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
        string userAgent =
            $"microsoft-extensions-ai-evaluation/{Constants.Version} (type=evaluator; subtype={evaluatorName})";

        AccessToken token =
            await serviceConfiguration.Credential.GetTokenAsync(
                new TokenRequestContext(scopes: ["https://management.azure.com/.default"]),
                cancellationToken).ConfigureAwait(false);

        httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        httpRequestMessage.Headers.UserAgent.ParseAdd(userAgent);

        if (httpRequestMessage.Content is not null)
        {
            httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
    }
}
