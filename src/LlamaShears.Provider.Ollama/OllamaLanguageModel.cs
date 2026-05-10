using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Content;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using ToolCall = LlamaShears.Core.Abstractions.Provider.ToolCall;

namespace LlamaShears.Provider.Ollama;

public partial class OllamaLanguageModel : ILanguageModel
{
    private static readonly JsonSerializerOptions _agentOptionsJson = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly IOllamaApiClient _client;
    private readonly OllamaProviderOptions _options;
    private readonly ModelConfiguration _configuration;
    private readonly ObjectPool<List<Message>> _messageListPool;
    private readonly IModelTextFormatter _textFormatter;
    private readonly ILogger<OllamaLanguageModel> _logger;

    public OllamaLanguageModel(
        IOllamaApiClientFactory clientFactory,
        ModelConfiguration configuration,
        IOptionsMonitor<OllamaProviderOptions> hostOptions,
        ObjectPool<List<Message>> messageListPool,
        IModelTextFormatter textFormatter,
        ILogger<OllamaLanguageModel> logger)
    {
        _configuration = configuration;
        _messageListPool = messageListPool;
        _textFormatter = textFormatter;
        _logger = logger;

        _options = AgentProviderOptions.Resolve(hostOptions.CurrentValue, configuration.Parameters, _agentOptionsJson);
        _client = clientFactory.CreateClient(_options);
    }

    public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
        ModelPrompt prompt,
        PromptOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var messages = _messageListPool.Get();
        try
        {
            foreach (var turn in prompt.Turns)
            {
                messages.Add(ToMessage(turn, _textFormatter));
            }

            var request = new ChatRequest
            {
                Model = _configuration.Id.Model,
                Stream = true,
                Messages = messages,
                Think = MapThinkLevel(_configuration.Think),
                KeepAlive = OllamaKeepAlive.Map(_options.KeepAlive),
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
                    LogThoughtReceived(_logger, _configuration.Id.Model, thinking);
                    yield return new OllamaThoughtFragment(thinking);
                }

                var content = chunk?.Message?.Content;
                if (!string.IsNullOrEmpty(content))
                {
                    LogTokenReceived(_logger, _configuration.Id.Model, content);
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
                        var callId = string.IsNullOrEmpty(ollamaCall.Id)
                            ? Guid.CreateVersion7().ToString()
                            : ollamaCall.Id;
                        LogToolCallReceived(_logger, _configuration.Id.Model, source, name);
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

    internal const string ToolNameSeparator = "__";

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

    private static Tool ToOllamaTool(string source, ToolDescriptor descriptor) =>
        new Tool
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

    private static Message ToMessage(ModelTurn turn, IModelTextFormatter textFormatter)
    {
        var message = new Message(MapRole(turn.Role), textFormatter.Format(turn));
        if (!turn.ToolCalls.IsDefaultOrEmpty)
        {
            message.ToolCalls = ToOllamaToolCalls(turn.ToolCalls);
        }
        if (turn.Role == ModelRole.Tool && turn.ToolCall is { } resolved)
        {
            message.ToolName = $"{resolved.Source}{ToolNameSeparator}{resolved.Name}";
        }
        if (!turn.Attachments.IsDefaultOrEmpty)
        {
            message.Images = ExtractImages(turn.Attachments);
        }
        return message;
    }

    private static string[]? ExtractImages(ImmutableArray<Attachment> attachments)
    {
        List<string>? images = null;
        foreach (var attachment in attachments)
        {
            if (attachment.Kind == AttachmentKind.Image)
            {
                images ??= [];
                images.Add(attachment.Base64Data);
            }
        }
        return images?.ToArray();
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
        ThinkLevel.None => false,
        ThinkLevel.Low => ThinkValue.Low,
        ThinkLevel.Medium => ThinkValue.Medium,
        ThinkLevel.High => ThinkValue.High,
        _ => null,
    };

    [LoggerMessage(Level = LogLevel.Debug, Message = "Token from {ModelId}: {Content}")]
    private static partial void LogTokenReceived(ILogger logger, string modelId, string content);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Thought from {ModelId}: {Content}")]
    private static partial void LogThoughtReceived(ILogger logger, string modelId, string content);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Tool call from {ModelId}: {Source}__{Name}")]
    private static partial void LogToolCallReceived(ILogger logger, string modelId, string source, string name);
}
