using System.Runtime.CompilerServices;
using LlamaShears.Provider.Abstractions;
using Microsoft.Extensions.ObjectPool;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace LlamaShears.Provider.Ollama;

public class OllamaLanguageModel : ILanguageModel
{
    private readonly IOllamaApiClient _client;
    private readonly ModelConfiguration _configuration;
    private readonly ObjectPool<List<Message>> _messageListPool;

    public OllamaLanguageModel(
        IOllamaApiClient client,
        ModelConfiguration configuration,
        ObjectPool<List<Message>> messageListPool)
    {
        _client = client;
        _configuration = configuration;
        _messageListPool = messageListPool;
    }

    public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
        ModelPrompt prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var messages = _messageListPool.Get();
        try
        {
            messages.AddRange(prompt.Turns.Select(ToMessage));

            var request = new ChatRequest
            {
                Model = _configuration.ModelId,
                Stream = true,
                Messages = messages
            };

            await foreach (var chunk in _client.ChatAsync(request, cancellationToken).ConfigureAwait(false))
            {
                if (chunk is null)
                {
                    continue;
                }

                yield return new OllamaResponseFragment(chunk.Message?.Content ?? string.Empty, chunk.Done);
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
}
