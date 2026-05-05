using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;

namespace LlamaShears.Provider.Ollama;

public sealed class OllamaEmbeddingProviderFactory : IEmbeddingProviderFactory
{
    private readonly IOllamaApiClient _client;
    private readonly IServiceProvider _serviceProvider;

    public OllamaEmbeddingProviderFactory(IOllamaApiClient client, IServiceProvider serviceProvider)
    {
        _client = client;
        _serviceProvider = serviceProvider;
    }

    public string Name => "OLLAMA";

    public async IAsyncEnumerable<ModelInfo> ListModelsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Ollama's /api/tags doesn't tag embedding-vs-chat capability,
        // so this listing is a superset; a richer filter (via
        // ShowModelAsync's Capabilities) can come later when we have a
        // reason to discriminate beyond what the operator picks via
        // config.
        var models = await _client.ListLocalModelsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var model in models)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new ModelInfo(
                ModelId: model.Name,
                DisplayName: model.Name,
                Description: model.Details?.Family,
                SupportedInputs: SupportedInputType.Text,
                SupportsReasoning: false,
                MaxContextWindow: 0);
        }
    }

    public IEmbeddingModel CreateModel(ModelConfiguration configuration)
        => ActivatorUtilities.CreateInstance<OllamaEmbeddingModel>(_serviceProvider, configuration);
}
