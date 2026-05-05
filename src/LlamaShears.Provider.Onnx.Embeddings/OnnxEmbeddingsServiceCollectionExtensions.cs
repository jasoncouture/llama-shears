using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Provider.Onnx.Embeddings;

public static class OnnxEmbeddingsServiceCollectionExtensions
{
    public const string DefaultConfigurationSection = "Providers:Onnx:Embeddings";

    public static IServiceCollection AddOnnxEmbeddingsProvider(
        this IServiceCollection services,
        string configurationSection = DefaultConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.AddOptions<OnnxEmbeddingsProviderOptions>()
            .BindConfiguration(configurationSection);

        services.AddSingleton<IEmbeddingProviderFactory, OnnxEmbeddingProviderFactory>();

        return services;
    }
}
