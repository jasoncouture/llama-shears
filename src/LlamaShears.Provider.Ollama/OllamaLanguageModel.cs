using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

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

    private static Message ToMessage(ModelTurn turn) => new(MapRole(turn.Role), turn.Content);

    private static ChatRole MapRole(ModelRole role) => role switch
    {
        ModelRole.System => ChatRole.System,
        ModelRole.User => ChatRole.User,
        ModelRole.Assistant => ChatRole.Assistant,
        ModelRole.FrameworkUser => ChatRole.User,
        ModelRole.FrameworkAssistant => ChatRole.Assistant,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported model role.")
    };

    private static ThinkValue? MapThinkLevel(ThinkLevel level) => level switch
    {
        ThinkLevel.None => null,
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
}
