using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;

namespace LlamaShears.Provider.Ollama;

public sealed class OllamaEmbeddingModel : IEmbeddingModel
{
    private readonly IOllamaApiClient _client;
    private readonly ModelConfiguration _configuration;

    public OllamaEmbeddingModel(
        IOllamaApiClientFactory clientFactory,
        ModelConfiguration configuration,
        IOptionsMonitor<OllamaProviderOptions> hostOptions)
    {
        _configuration = configuration;

        var merged = AgentProviderOptions.Resolve(hostOptions.CurrentValue, configuration.AgentOptions);
        _client = clientFactory.CreateClient(merged);
    }

    public async ValueTask<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);

        var response = await _client
            .EmbedAsync(BuildRequest([text]), cancellationToken)
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
            inputs.Add(texts[i]);
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

    private EmbedRequest BuildRequest(List<string> inputs) => new()
    {
        Model = _configuration.ModelId,
        Input = inputs,
        KeepAlive = OllamaKeepAlive.Map(_configuration.KeepAlive),
    };
}
