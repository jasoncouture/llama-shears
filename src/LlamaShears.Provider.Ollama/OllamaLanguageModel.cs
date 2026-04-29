using System.Runtime.CompilerServices;
using LlamaShears.Provider.Abstractions;
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
                Options = new RequestOptions
                {
                    Seed = Random.Shared.Next(),
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
            }
        }
        finally
        {
            _messageListPool.Return(messages);
        }
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Token from {ModelId}: {Content}")]
    private static partial void LogTokenReceived(ILogger logger, string modelId, string content);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Thought from {ModelId}: {Content}")]
    private static partial void LogThoughtReceived(ILogger logger, string modelId, string content);
}
