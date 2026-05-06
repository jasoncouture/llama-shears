using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using ToolCall = LlamaShears.Core.Abstractions.Provider.ToolCall;

namespace LlamaShears.Provider.Ollama;

public partial class OllamaLanguageModel : ILanguageModel
{
    private readonly IOllamaApiClient _client;
    private readonly ModelConfiguration _configuration;
    private readonly ObjectPool<List<Message>> _messageListPool;
    private readonly ILogger<OllamaLanguageModel> _logger;

    public OllamaLanguageModel(
        IOllamaApiClient client,
        ModelConfiguration configuration,
        ObjectPool<List<Message>> messageListPool,
        ILogger<OllamaLanguageModel> logger)
    {
        _client = client;
        _configuration = configuration;
        _messageListPool = messageListPool;
        _logger = logger;
    }

    public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
        ModelPrompt prompt,
        PromptOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var messages = _messageListPool.Get();
        try
        {
            // Thought turns are kept in agent context for visibility but
            // must not be resubmitted to the model — see ModelRole.Thought.
            foreach (var turn in prompt.Turns)
            {
                if (turn.Role == ModelRole.Thought)
                {
                    continue;
                }

                messages.Add(ToMessage(turn));
            }

            var request = new ChatRequest
            {
                Model = _configuration.ModelId,
                Stream = true,
                Messages = messages,
                Think = MapThinkLevel(_configuration.Think),
                KeepAlive = MapKeepAlive(_configuration.KeepAlive),
                Tools = BuildTools(options),
                Options = new RequestOptions
                {
                    Seed = Random.Shared.Next(),
                    NumCtx = _configuration.ContextLength,
                    NumPredict = ResolveTokenLimit(options),
                },
            };

            await foreach (var chunk in _client.ChatAsync(request, cancellationToken).ConfigureAwait(false))
            {
                var thinking = chunk?.Message?.Thinking;
                if (!string.IsNullOrEmpty(thinking))
                {
                    LogThoughtReceived(_logger, _configuration.ModelId, thinking);
                    yield return new OllamaThoughtFragment(thinking);
                }

                var content = chunk?.Message?.Content;
                if (!string.IsNullOrEmpty(content))
                {
                    LogTokenReceived(_logger, _configuration.ModelId, content);
                    yield return new OllamaResponseFragment(content);
                }

                var toolCalls = chunk?.Message?.ToolCalls;
                if (toolCalls is not null)
                {
                    foreach (var ollamaCall in toolCalls)
                    {
                        if (ollamaCall?.Function?.Name is not { } flatName)
                        {
                            continue;
                        }

                        var (source, name) = SplitToolName(flatName);
                        var argumentsJson = SerializeArguments(ollamaCall.Function.Arguments);
                        // Ollama's ToolCall.Id is optional and we routinely
                        // see it null. Synthesize one so the UI (and any
                        // future positional-tolerant routing) has a stable
                        // handle for the call -> result pairing.
                        var callId = string.IsNullOrEmpty(ollamaCall.Id)
                            ? Guid.CreateVersion7().ToString()
                            : ollamaCall.Id;
                        LogToolCallReceived(_logger, _configuration.ModelId, source, name);
                        yield return new OllamaToolCallFragment(
                            new ToolCall(source, name, argumentsJson, callId));
                    }
                }

                if (chunk is ChatDoneResponseStream done)
                {
                    yield return new OllamaCompletionFragment(done.PromptEvalCount + done.EvalCount);
                }
            }
        }
        finally
        {
            _messageListPool.Return(messages);
        }
    }

    // Ollama's chat API takes a flat tool list, so we collapse the
    // (source, name) pair the abstraction layer hands us into a single
    // wire name as `source__name`, and split the same way when the
    // model echoes a function name back in a tool call.
    internal const string ToolNameSeparator = "__";

    private static (string Source, string Name) SplitToolName(string flatName)
    {
        var separatorIndex = flatName.IndexOf(ToolNameSeparator, StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            // No prefix — the model emitted a name that doesn't follow
            // our flattening convention. Surface it with an empty source
            // so downstream dispatch can refuse loudly rather than
            // guessing which server it belongs to.
            return (string.Empty, flatName);
        }
        return (
            flatName[..separatorIndex],
            flatName[(separatorIndex + ToolNameSeparator.Length)..]);
    }

    private static string SerializeArguments(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return "{}";
        }
        return JsonSerializer.Serialize(arguments);
    }

    private static IEnumerable<object>? BuildTools(PromptOptions? options)
    {
        if (options?.Tools is not { IsDefaultOrEmpty: false } groups)
        {
            return null;
        }

        var tools = new List<object>();
        foreach (var group in groups)
        {
            if (group.Tools.IsDefaultOrEmpty)
            {
                continue;
            }
            foreach (var descriptor in group.Tools)
            {
                tools.Add(ToOllamaTool(group.Source, descriptor));
            }
        }
        return tools.Count == 0 ? null : tools;
    }

    private static Tool ToOllamaTool(string source, ToolDescriptor descriptor) => new()
    {
        Type = "function",
        Function = new Function
        {
            Name = $"{source}{ToolNameSeparator}{descriptor.Name}",
            Description = descriptor.Description,
            Parameters = ToOllamaParameters(descriptor.Parameters),
        },
    };

    private static Parameters? ToOllamaParameters(ImmutableArray<ToolParameter> parameters)
    {
        if (parameters.IsDefaultOrEmpty)
        {
            return null;
        }

        var properties = new Dictionary<string, Property>(StringComparer.Ordinal);
        List<string>? required = null;
        foreach (var p in parameters)
        {
            properties[p.Name] = new Property
            {
                Type = p.Type,
                Description = p.Description,
            };
            if (p.Required)
            {
                required ??= [];
                required.Add(p.Name);
            }
        }

        return new Parameters
        {
            Type = "object",
            Properties = properties,
            Required = required,
        };
    }

    // Per-call options.TokenLimit overrides config.TokenLimit; both must be
    // > 0 to be meaningful, otherwise we hand null to OllamaSharp and let
    // the server pick its default.
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

    private static Message ToMessage(ModelTurn turn)
    {
        var message = new Message(MapRole(turn.Role), turn.Content);
        if (!turn.ToolCalls.IsDefaultOrEmpty)
        {
            message.ToolCalls = ToOllamaToolCalls(turn.ToolCalls);
        }
        if (turn.Role == ModelRole.Tool && !string.IsNullOrEmpty(turn.ToolName))
        {
            message.ToolName = turn.ToolName;
        }
        return message;
    }

    private static List<Message.ToolCall> ToOllamaToolCalls(ImmutableArray<ToolCall> calls)
    {
        var list = new List<Message.ToolCall>();
        foreach (var call in calls)
        {
            list.Add(new Message.ToolCall
            {
                Id = call.CallId,
                Function = new Message.Function
                {
                    Name = $"{call.Source}{ToolNameSeparator}{call.Name}",
                    Arguments = DeserializeArguments(call.ArgumentsJson),
                },
            });
        }
        return list;
    }

    private static Dictionary<string, object?>? DeserializeArguments(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return null;
        }
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson);
    }

    private static ChatRole MapRole(ModelRole role) => role switch
    {
        ModelRole.System => ChatRole.System,
        ModelRole.User => ChatRole.User,
        ModelRole.Assistant => ChatRole.Assistant,
        ModelRole.FrameworkUser => ChatRole.User,
        ModelRole.FrameworkAssistant => ChatRole.Assistant,
        ModelRole.Tool => ChatRole.Tool,
        ModelRole.SystemEphemeral => ChatRole.User,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported model role.")
    };

    private static ThinkValue? MapThinkLevel(ThinkLevel level) => level switch
    {
        // Explicit false disables thinking; null would let the model's default
        // through, which still emits thought tokens for reasoning-capable models.
        ThinkLevel.None => false,
        ThinkLevel.Low => ThinkValue.Low,
        ThinkLevel.Medium => ThinkValue.Medium,
        ThinkLevel.High => ThinkValue.High,
        _ => null,
    };

    private static string? MapKeepAlive(TimeSpan? keepAlive)
    {
        if (keepAlive is not { } span)
        {
            return null;
        }

        if (span == TimeSpan.Zero)
        {
            return "0";
        }

        if (span < TimeSpan.Zero)
        {
            return "-1";
        }

        // Ollama wants whole minutes with the "m" suffix for inactivity timeouts.
        return $"{(long)Math.Round(span.TotalMinutes)}m";
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Token from {ModelId}: {Content}")]
    private static partial void LogTokenReceived(ILogger logger, string modelId, string content);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Thought from {ModelId}: {Content}")]
    private static partial void LogThoughtReceived(ILogger logger, string modelId, string content);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Tool call from {ModelId}: {Source}__{Name}")]
    private static partial void LogToolCallReceived(ILogger logger, string modelId, string source, string name);
}
