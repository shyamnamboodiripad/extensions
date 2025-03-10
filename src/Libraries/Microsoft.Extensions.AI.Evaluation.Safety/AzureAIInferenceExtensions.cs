// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Azure.AI.Inference;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal static class AzureAIInferenceExtensions
{
    private static ChatRole ChatRoleDeveloper { get; } = new("developer");

    internal static IEnumerable<ChatRequestMessage> ToAzureAIInferenceChatRequestMessages(this IEnumerable<ChatMessage> inputs)
    {
        // Maps all of the M.E.AI types to the corresponding AzureAI types.
        // Unrecognized or non-processable content is ignored.

        foreach (ChatMessage input in inputs)
        {
            if (input.Role == ChatRole.System)
            {
                yield return new ChatRequestSystemMessage(input.Text ?? string.Empty);
            }
            else if (input.Role == ChatRoleDeveloper)
            {
                yield return new ChatRequestDeveloperMessage(input.Text ?? string.Empty);
            }
            else if (input.Role == ChatRole.Tool)
            {
                foreach (AIContent item in input.Contents)
                {
                    if (item is FunctionResultContent resultContent)
                    {
                        string? result = resultContent.Result as string;
                        if (result is null && resultContent.Result is not null)
                        {
                            try
                            {
                                JsonTypeInfo typeInfo = AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(object));
                                result = JsonSerializer.Serialize(resultContent.Result, typeInfo);
                            }
                            catch (NotSupportedException)
                            {
                                // If the type can't be serialized, skip it.
                            }
                        }

                        yield return new ChatRequestToolMessage(result ?? string.Empty, resultContent.CallId);
                    }
                }
            }
            else if (input.Role == ChatRole.User)
            {
                yield return input.Contents.All(c => c is TextContent) ?
                    new ChatRequestUserMessage(string.Concat(input.Contents)) :
                    new ChatRequestUserMessage(GetContentParts(input.Contents));
            }
            else if (input.Role == ChatRole.Assistant)
            {
                // TODO: ChatRequestAssistantMessage only enables text content currently.
                // Update it with other content types when it supports that.
                ChatRequestAssistantMessage message = new(string.Concat(input.Contents.Where(c => c is TextContent)));

                foreach (var content in input.Contents)
                {
                    if (content is FunctionCallContent { CallId: not null } callRequest)
                    {
                        JsonTypeInfo typeInfo = AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(IDictionary<string, object>));

                        message.ToolCalls.Add(new ChatCompletionsToolCall(
                             callRequest.CallId,
                             new FunctionCall(
                                 callRequest.Name,
                                 JsonSerializer.Serialize(callRequest.Arguments, typeInfo))));
                    }
                }

                yield return message;
            }
        }

        static List<ChatMessageContentItem> GetContentParts(IList<AIContent> contents)
        {
            Debug.Assert(contents is { Count: > 0 }, "Expected non-empty contents");

            List<ChatMessageContentItem> parts = [];
            foreach (var content in contents)
            {
                switch (content)
                {
                    case TextContent textContent:
                        parts.Add(new ChatMessageTextContentItem(textContent.Text));
                        break;

                    case DataContent dataContent when dataContent.MediaTypeStartsWith("image/"):
                        if (dataContent.Data.HasValue)
                        {
                            parts.Add(new ChatMessageImageContentItem(BinaryData.FromBytes(dataContent.Data.Value), dataContent.MediaType));
                        }
                        else if (dataContent.Uri is string uri)
                        {
                            parts.Add(new ChatMessageImageContentItem(new Uri(uri)));
                        }

                        break;

                    case DataContent dataContent when dataContent.MediaTypeStartsWith("audio/"):
                        if (dataContent.Data.HasValue)
                        {
                            AudioContentFormat format;
                            if (dataContent.MediaTypeStartsWith("audio/mpeg"))
                            {
                                format = AudioContentFormat.Mp3;
                            }
                            else if (dataContent.MediaTypeStartsWith("audio/wav"))
                            {
                                format = AudioContentFormat.Wav;
                            }
                            else
                            {
                                break;
                            }

                            parts.Add(new ChatMessageAudioContentItem(BinaryData.FromBytes(dataContent.Data.Value), format));
                        }
                        else if (dataContent.Uri is string uri)
                        {
                            parts.Add(new ChatMessageAudioContentItem(new Uri(uri)));
                        }

                        break;
                }
            }

            return parts;
        }
    }
}
