using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LlamaShears.Core.Abstractions.Content;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolCall = LlamaShears.Core.Abstractions.Provider.ToolCall;

namespace LlamaShears.Provider.OpenAI;

public partial class OpenAILanguageModel : ILanguageModel
{
    // Same flatten convention as the Ollama provider so multi-MCP
    // tool routing is identical across backends.
    internal const string ToolNameSeparator = "__";

    private const string ChatCompletionsPath = "v1/chat/completions";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAIProviderOptions _options;
    private readonly ModelConfiguration _configuration;
    private readonly ILogger<OpenAILanguageModel> _logger;

    public OpenAILanguageModel(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<OpenAIProviderOptions> hostOptions,
        ModelConfiguration configuration,
        ILogger<OpenAILanguageModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _options = AgentProviderOptions.Resolve(hostOptions.CurrentValue, configuration.AgentOptions);
    }

    public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
        ModelPrompt prompt,
        PromptOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requestBody = BuildRequestBody(prompt, options);
        var requestUri = new Uri(_options.BaseUri, ChatCompletionsPath);

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Content = new StringContent(
            requestBody.ToJsonString(_jsonOptions),
            Encoding.UTF8,
            "application/json");
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var httpClient = _httpClientFactory.CreateClient(nameof(OpenAILanguageModel));
        httpClient.Timeout = _options.RequestTimeout;

        using var response = await httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            LogRequestFailed(_logger, _configuration.ModelId, (int)response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"OpenAI-compatible chat request failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {errorBody}");
        }

        // Streaming tool calls arrive as deltas — each delta carries
        // an index and possibly partial id/name/arguments. We
        // accumulate per-index and emit a single ToolCall fragment
        // when finish_reason hits.
        var toolCallAccumulator = new Dictionary<int, ToolCallAccumulator>();
        int? totalTokens = null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }
            if (line.Length == 0 || !line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = line.AsSpan(5).TrimStart().ToString();
            if (payload == "[DONE]")
            {
                break;
            }

            JsonNode? eventNode;
            try
            {
                eventNode = JsonNode.Parse(payload);
            }
            catch (JsonException ex)
            {
                LogMalformedEvent(_logger, _configuration.ModelId, ex.Message);
                continue;
            }
            if (eventNode is not JsonObject eventObject)
            {
                continue;
            }

            // OpenAI emits usage on a separate trailer chunk when
            // `stream_options.include_usage` is set; some backends
            // (llama-server) include it on the final delta inline.
            if (eventObject["usage"] is JsonObject usage)
            {
                totalTokens =
                    GetIntOrZero(usage, "prompt_tokens") +
                    GetIntOrZero(usage, "completion_tokens");
            }

            if (eventObject["choices"] is not JsonArray choices || choices.Count == 0)
            {
                continue;
            }

            foreach (var fragment in HandleChoices(choices, toolCallAccumulator))
            {
                yield return fragment;
            }
        }

        // Drain any tool calls that didn't get flushed by an explicit
        // finish_reason — some backends end the stream without one.
        foreach (var pending in toolCallAccumulator.Values.OrderBy(static a => a.Index))
        {
            if (pending.TryBuild(_configuration.ModelId, _logger, out var call))
            {
                yield return new OpenAIToolCallFragment(call);
            }
        }
        toolCallAccumulator.Clear();

        if (totalTokens is { } tokens)
        {
            yield return new OpenAICompletionFragment(tokens);
        }
    }

    private IEnumerable<IModelResponseFragment> HandleChoices(
        JsonArray choices,
        Dictionary<int, ToolCallAccumulator> toolCallAccumulator)
    {
        var emitted = new List<IModelResponseFragment>();
        foreach (var choiceNode in choices)
        {
            if (choiceNode is not JsonObject choice)
            {
                continue;
            }

            if (choice["delta"] is JsonObject delta)
            {
                var content = delta["content"]?.GetValue<string?>();
                if (!string.IsNullOrEmpty(content))
                {
                    LogTokenReceived(_logger, _configuration.ModelId, content);
                    emitted.Add(new OpenAIResponseFragment(content));
                }

                // Reasoning surface naming varies: llama-server with
                // jinja templates emits `reasoning_content`, OpenAI's
                // o-series uses the same key, some adapters use
                // `reasoning`. Accept either.
                var reasoning = delta["reasoning_content"]?.GetValue<string?>()
                    ?? delta["reasoning"]?.GetValue<string?>();
                if (!string.IsNullOrEmpty(reasoning))
                {
                    LogThoughtReceived(_logger, _configuration.ModelId, reasoning);
                    emitted.Add(new OpenAIThoughtFragment(reasoning));
                }

                if (delta["tool_calls"] is JsonArray toolDeltas)
                {
                    foreach (var deltaNode in toolDeltas)
                    {
                        if (deltaNode is JsonObject toolDelta)
                        {
                            AccumulateToolCall(toolDelta, toolCallAccumulator);
                        }
                    }
                }
            }

            // Flush tool calls when this choice ends with tool_calls.
            // Some backends (notably llama-server) terminate the stream
            // without a finish_reason; the post-loop drain covers that.
            var finishReason = choice["finish_reason"]?.GetValue<string?>();
            if (string.Equals(finishReason, "tool_calls", StringComparison.Ordinal))
            {
                foreach (var pending in toolCallAccumulator.Values.OrderBy(static a => a.Index))
                {
                    if (pending.TryBuild(_configuration.ModelId, _logger, out var call))
                    {
                        emitted.Add(new OpenAIToolCallFragment(call));
                    }
                }
                toolCallAccumulator.Clear();
            }
        }
        return emitted;
    }

    private static void AccumulateToolCall(
        JsonObject delta,
        Dictionary<int, ToolCallAccumulator> accumulator)
    {
        // OpenAI's streaming protocol uses `index` to disambiguate
        // which tool the delta belongs to. Some backends omit it on
        // single-tool calls — fall back to index 0.
        var index = delta["index"]?.GetValue<int?>() ?? 0;
        if (!accumulator.TryGetValue(index, out var entry))
        {
            entry = new ToolCallAccumulator(index);
            accumulator[index] = entry;
        }

        var id = delta["id"]?.GetValue<string?>();
        if (!string.IsNullOrEmpty(id))
        {
            entry.Id = id;
        }

        if (delta["function"] is JsonObject function)
        {
            var name = function["name"]?.GetValue<string?>();
            if (!string.IsNullOrEmpty(name))
            {
                entry.Name = name;
            }

            var argumentsChunk = function["arguments"]?.GetValue<string?>();
            if (!string.IsNullOrEmpty(argumentsChunk))
            {
                entry.Arguments.Append(argumentsChunk);
            }
        }
    }

    private JsonObject BuildRequestBody(ModelPrompt prompt, PromptOptions? options)
    {
        var messages = new JsonArray();
        // Thought turns stay in agent context for visibility but are
        // never resubmitted — same rule as the Ollama provider.
        foreach (var turn in prompt.Turns)
        {
            if (turn.Role == ModelRole.Thought)
            {
                continue;
            }
            messages.Add(ToMessage(turn));
        }

        var body = new JsonObject
        {
            ["model"] = _configuration.ModelId,
            ["stream"] = true,
            ["messages"] = messages,
        };

        var tokenLimit = ResolveTokenLimit(options);
        if (tokenLimit is { } limit)
        {
            // `max_tokens` is deprecated in favor of
            // `max_completion_tokens` on api.openai.com; llama-server
            // and most compat servers still accept the older field.
            // Send the older one for max compatibility — callers who
            // need the new field can override via ExtraRequestParams.
            body["max_tokens"] = limit;
        }

        var tools = BuildTools(options);
        if (tools is not null)
        {
            body["tools"] = tools;
        }

        // Ask for usage on the trailer chunk; servers that don't
        // support it ignore the field, ones that do (OpenAI,
        // llama-server) emit a final usage delta.
        body["stream_options"] = new JsonObject { ["include_usage"] = true };

        // Layer extras last so callers can override anything above —
        // including swapping `max_tokens` for `max_completion_tokens`
        // when targeting api.openai.com directly.
        MergeExtras(body, _options.ExtraRequestParams);

        return body;
    }

    private static void MergeExtras(JsonObject target, JsonObject extras)
    {
        foreach (var (key, value) in extras)
        {
            // Clone — extras is config-level state and the merged body
            // becomes part of an outgoing request; sharing nodes risks
            // double-parenting on the next call.
            target[key] = value?.DeepClone();
        }
    }

    private static JsonObject ToMessage(ModelTurn turn)
    {
        var role = MapRole(turn.Role);
        var message = new JsonObject { ["role"] = role };

        if (turn.Role == ModelRole.Tool)
        {
            // Tool result message: link via tool_call_id and serialize
            // text content; OpenAI doesn't accept attachments here.
            if (turn.ToolCall is { CallId: { Length: > 0 } callId })
            {
                message["tool_call_id"] = callId;
            }
            message["content"] = turn.Content;
            return message;
        }

        if (!turn.Attachments.IsDefaultOrEmpty && turn.Role == ModelRole.User)
        {
            message["content"] = BuildMultimodalContent(turn);
        }
        else
        {
            message["content"] = turn.Content;
        }

        if (!turn.ToolCalls.IsDefaultOrEmpty)
        {
            var calls = new JsonArray();
            foreach (var call in turn.ToolCalls)
            {
                calls.Add(new JsonObject
                {
                    ["id"] = call.CallId ?? Guid.CreateVersion7().ToString(),
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = $"{call.Source}{ToolNameSeparator}{call.Name}",
                        ["arguments"] = call.ArgumentsJson,
                    },
                });
            }
            message["tool_calls"] = calls;
        }

        return message;
    }

    private static JsonArray BuildMultimodalContent(ModelTurn turn)
    {
        // OpenAI's vision shape: content is an array of typed parts;
        // text first, image_url parts after. Non-image attachments are
        // dropped here — they belong to whatever non-vision channel
        // the provider grows next.
        var parts = new JsonArray
        {
            new JsonObject { ["type"] = "text", ["text"] = turn.Content },
        };
        foreach (var attachment in turn.Attachments)
        {
            if (attachment.Kind != AttachmentKind.Image)
            {
                continue;
            }
            var dataUrl = $"data:{attachment.MimeType};base64,{attachment.Base64Data}";
            parts.Add(new JsonObject
            {
                ["type"] = "image_url",
                ["image_url"] = new JsonObject { ["url"] = dataUrl },
            });
        }
        return parts;
    }

    private static string MapRole(ModelRole role) => role switch
    {
        ModelRole.System => "system",
        ModelRole.User or ModelRole.FrameworkUser or ModelRole.SystemEphemeral => "user",
        ModelRole.Assistant or ModelRole.FrameworkAssistant => "assistant",
        ModelRole.Tool => "tool",
        // Thought is filtered upstream; if it ever reaches here something
        // is wrong with the caller — fall through to assistant rather
        // than silently dropping.
        ModelRole.Thought => "assistant",
        _ => "user",
    };

    private static JsonArray? BuildTools(PromptOptions? options)
    {
        if (options?.Tools is not { IsDefaultOrEmpty: false } groups)
        {
            return null;
        }

        var tools = new JsonArray();
        foreach (var group in groups)
        {
            if (group.Tools.IsDefaultOrEmpty)
            {
                continue;
            }
            foreach (var descriptor in group.Tools)
            {
                tools.Add(ToOpenAITool(group.Source, descriptor));
            }
        }
        return tools.Count == 0 ? null : tools;
    }

    private static JsonObject ToOpenAITool(string source, ToolDescriptor descriptor)
    {
        var properties = new JsonObject();
        var required = new JsonArray();
        foreach (var parameter in descriptor.Parameters)
        {
            properties[parameter.Name] = new JsonObject
            {
                ["type"] = parameter.Type,
                ["description"] = parameter.Description,
            };
            if (parameter.Required)
            {
                required.Add(parameter.Name);
            }
        }

        var parametersObject = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
        };
        if (required.Count > 0)
        {
            parametersObject["required"] = required;
        }

        return new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = $"{source}{ToolNameSeparator}{descriptor.Name}",
                ["description"] = descriptor.Description,
                ["parameters"] = parametersObject,
            },
        };
    }

    private static (string Source, string Name) SplitToolName(string flatName)
    {
        var separatorIndex = flatName.IndexOf(ToolNameSeparator, StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return (string.Empty, flatName);
        }
        return (
            flatName[..separatorIndex],
            flatName[(separatorIndex + ToolNameSeparator.Length)..]);
    }

    private int? ResolveTokenLimit(PromptOptions? options)
    {
        if (options?.TokenLimit is { } overrideLimit && overrideLimit > 0)
        {
            return overrideLimit;
        }
        if (_configuration.TokenLimit > 0)
        {
            return _configuration.TokenLimit;
        }
        return null;
    }

    private static int GetIntOrZero(JsonObject node, string key) =>
        node[key] is JsonValue value && value.TryGetValue<int>(out var integer) ? integer : 0;

    private sealed class ToolCallAccumulator
    {
        public ToolCallAccumulator(int index)
        {
            Index = index;
        }

        public int Index { get; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public StringBuilder Arguments { get; } = new();

        public bool TryBuild(string modelId, ILogger logger, out ToolCall call)
        {
            if (string.IsNullOrEmpty(Name))
            {
                LogIncompleteToolCall(logger, modelId, Index);
                call = default!;
                return false;
            }
            var (source, name) = SplitToolName(Name);
            var arguments = Arguments.Length == 0 ? "{}" : Arguments.ToString();
            // Synthesize an id when the backend doesn't surface one
            // (some llama-server builds, some MCP shims) so paired
            // tool-result messages have a stable handle.
            var callId = string.IsNullOrEmpty(Id) ? Guid.CreateVersion7().ToString() : Id;
            LogToolCallReceived(logger, modelId, source, name);
            call = new ToolCall(source, name, arguments, callId);
            return true;
        }
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "OpenAI[{ModelId}] token: {Content}")]
    private static partial void LogTokenReceived(ILogger logger, string modelId, string content);

    [LoggerMessage(Level = LogLevel.Trace, Message = "OpenAI[{ModelId}] thought: {Content}")]
    private static partial void LogThoughtReceived(ILogger logger, string modelId, string content);

    [LoggerMessage(Level = LogLevel.Debug, Message = "OpenAI[{ModelId}] tool call: {Source}/{Name}")]
    private static partial void LogToolCallReceived(ILogger logger, string modelId, string source, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "OpenAI[{ModelId}] dropped malformed event: {Reason}")]
    private static partial void LogMalformedEvent(ILogger logger, string modelId, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "OpenAI[{ModelId}] tool call delta at index {Index} ended without a name; dropping.")]
    private static partial void LogIncompleteToolCall(ILogger logger, string modelId, int index);

    [LoggerMessage(Level = LogLevel.Error, Message = "OpenAI[{ModelId}] chat request failed with status {Status}. Body: {Body}")]
    private static partial void LogRequestFailed(ILogger logger, string modelId, int status, string body);
}
