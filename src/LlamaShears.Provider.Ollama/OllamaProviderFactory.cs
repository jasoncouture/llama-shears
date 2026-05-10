using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LlamaShears.Provider.Ollama;

public class OllamaProviderFactory : IProviderFactory
{
    public const string ProviderName = "ollama";

    private readonly IOllamaApiClientFactory _clientFactory;
    private readonly IOptionsMonitor<OllamaProviderOptions> _hostOptions;
    private readonly IServiceProvider _serviceProvider;

    public OllamaProviderFactory(
        IOllamaApiClientFactory clientFactory,
        IOptionsMonitor<OllamaProviderOptions> hostOptions,
        IServiceProvider serviceProvider)
    {
        _clientFactory = clientFactory;
        _hostOptions = hostOptions;
        _serviceProvider = serviceProvider;
    }

    public string Name => ProviderName;

    public async IAsyncEnumerable<ModelInfo> ListModelsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
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

    public async ValueTask<ValidationResult?> ValidateAsync(ModelConfiguration configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configuration.ModelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration.ModelId.Model);

        if (!string.Equals(configuration.ModelId.Provider, Name, StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                $"Provider '{configuration.ModelId.Provider}' does not match this factory ('{Name}').",
                [nameof(ModelConfiguration.ModelId)]);
        }

        var client = _clientFactory.CreateClient(_hostOptions.CurrentValue);
        var models = await client.ListLocalModelsAsync(cancellationToken).ConfigureAwait(false);
        foreach (var model in models)
        {
            if (string.Equals(model.Name, configuration.ModelId.Model, StringComparison.Ordinal))
            {
                return ValidationResult.Success;
            }
        }
        return new ValidationResult(
            $"Ollama provider does not have a model named '{configuration.ModelId.Model}'.",
            [nameof(ModelConfiguration.ModelId)]);
    }

    public ILanguageModel CreateModel(ModelConfiguration configuration)
        => ActivatorUtilities.CreateInstance<OllamaLanguageModel>(_serviceProvider, configuration);
}
