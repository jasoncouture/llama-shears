using System.Runtime.CompilerServices;
using LlamaShears.Provider.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;

namespace LlamaShears.Provider.Ollama;

/// <summary>
/// <see cref="IProviderFactory"/> for Ollama-hosted models.
/// </summary>
public class OllamaProviderFactory : IProviderFactory
{
    private readonly IOllamaApiClient _client;
    private readonly IServiceProvider _serviceProvider;

    public OllamaProviderFactory(IOllamaApiClient client, IServiceProvider serviceProvider)
    {
        _client = client;
        _serviceProvider = serviceProvider;
    }

    public string Name => "OLLAMA";

    public async IAsyncEnumerable<ModelInfo> ListModelsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
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

    public ILanguageModel CreateModel(ModelConfiguration configuration)
        => ActivatorUtilities.CreateInstance<OllamaLanguageModel>(_serviceProvider, configuration);
}
