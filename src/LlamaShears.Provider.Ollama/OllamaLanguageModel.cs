using System.Runtime.CompilerServices;
using LlamaShears.Provider.Abstractions;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace LlamaShears.Provider.Ollama;

/// <summary>
/// <see cref="ILanguageModel"/> implementation backed by an Ollama server.
/// </summary>
public class OllamaLanguageModel : ILanguageModel
{
    private readonly IOllamaApiClient _client;
    private readonly ModelConfiguration _configuration;

    public OllamaLanguageModel(IOllamaApiClient client, ModelConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
        ModelPrompt prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new ChatRequest
        {
            Model = _configuration.ModelId,
            Stream = true,
            Messages = prompt.Turns.Select(ToMessage).ToList()
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

    private static Message ToMessage(ModelTurn turn) => new(MapRole(turn.Role), turn.Content);

    private static ChatRole MapRole(ModelRole role) => role switch
    {
        ModelRole.System => ChatRole.System,
        ModelRole.User => ChatRole.User,
        ModelRole.Assistant => ChatRole.Assistant,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported model role.")
    };
}
