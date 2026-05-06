using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LlamaShears.Provider.Ollama;

public sealed class OllamaEmbeddingProviderFactory : IEmbeddingProviderFactory
{
    private readonly IOllamaApiClientFactory _clientFactory;
    private readonly IOptionsMonitor<OllamaProviderOptions> _hostOptions;
    private readonly IServiceProvider _serviceProvider;

    public OllamaEmbeddingProviderFactory(
        IOllamaApiClientFactory clientFactory,
        IOptionsMonitor<OllamaProviderOptions> hostOptions,
        IServiceProvider serviceProvider)
    {
        _clientFactory = clientFactory;
        _hostOptions = hostOptions;
        _serviceProvider = serviceProvider;
    }

    public string Name => OllamaProviderFactory.ProviderName;

    public async IAsyncEnumerable<ModelInfo> ListModelsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Ollama's /api/tags doesn't tag embedding-vs-chat capability,
        // so this listing is a superset; a richer filter (via
        // ShowModelAsync's Capabilities) can come later when we have a
        // reason to discriminate beyond what the operator picks via
        // config.
        var client = _clientFactory.CreateClient(_hostOptions.CurrentValue);
        var models = await client.ListLocalModelsAsync(cancellationToken).ConfigureAwait(false);

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
