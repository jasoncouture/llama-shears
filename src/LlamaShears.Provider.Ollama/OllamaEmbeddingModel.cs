using LlamaShears.Core.Abstractions.Provider;
using OllamaSharp;
using OllamaSharp.Models;

namespace LlamaShears.Provider.Ollama;

public sealed class OllamaEmbeddingModel : IEmbeddingModel
{
    private readonly IOllamaApiClient _client;
    private readonly ModelConfiguration _configuration;

    public OllamaEmbeddingModel(IOllamaApiClient client, ModelConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async ValueTask<ReadOnlyMemory<float>> EmbedAsync(string text, EmbeddingPurpose purpose, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);

        var response = await _client
            .EmbedAsync(BuildRequest([Decorate(text, purpose)]), cancellationToken)
            .ConfigureAwait(false);
        var embeddings = response?.Embeddings;
        if (embeddings is null || embeddings.Count == 0)
        {
            throw new InvalidOperationException(
                $"Ollama returned no embeddings for model '{_configuration.ModelId}'.");
        }
        return embeddings[0];
    }

    public async ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (texts.Count == 0)
        {
            return [];
        }

        var inputs = new List<string>(texts.Count);
        for (var i = 0; i < texts.Count; i++)
        {
            inputs.Add(Decorate(texts[i], purpose));
        }

        var response = await _client.EmbedAsync(BuildRequest(inputs), cancellationToken).ConfigureAwait(false);
        var embeddings = response?.Embeddings;
        if (embeddings is null || embeddings.Count != texts.Count)
        {
            throw new InvalidOperationException(
                $"Ollama returned {embeddings?.Count ?? 0} embeddings for {texts.Count} inputs (model '{_configuration.ModelId}').");
        }

        var results = new ReadOnlyMemory<float>[embeddings.Count];
        for (var i = 0; i < embeddings.Count; i++)
        {
            results[i] = embeddings[i];
        }
        return results;
    }

    // Asymmetric retrieval models expect explicit task prefixes — without
    // them every input is encoded the same way and cosine scores collapse
    // toward each other. Detection is by ModelId substring because Ollama
    // doesn't expose a "I am asymmetric, here is my prompt schema" hint;
    // when we add another asymmetric family we extend the table.
    private string Decorate(string text, EmbeddingPurpose purpose)
    {
        var modelId = _configuration.ModelId;
        if (modelId.Contains("embeddinggemma", StringComparison.OrdinalIgnoreCase))
        {
            return purpose switch
            {
                EmbeddingPurpose.Query => $"task: search result | query: {text}",
                EmbeddingPurpose.Document => $"title: none | text: {text}",
                _ => text,
            };
        }
        if (modelId.Contains("nomic-embed-text", StringComparison.OrdinalIgnoreCase))
        {
            return purpose switch
            {
                EmbeddingPurpose.Query => $"search_query: {text}",
                EmbeddingPurpose.Document => $"search_document: {text}",
                _ => text,
            };
        }
        return text;
    }

    private EmbedRequest BuildRequest(List<string> inputs) => new()
    {
        Model = _configuration.ModelId,
        Input = inputs,
        KeepAlive = OllamaKeepAlive.Map(_configuration.KeepAlive),
    };
}
