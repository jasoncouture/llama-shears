using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace LlamaShears.Provider.Ollama;

public static class OllamaServiceCollectionExtensions
{
    public const string DefaultConfigurationSection = "Providers:Ollama";

    public static IServiceCollection AddOllamaProvider(
        this IServiceCollection services,
        string configurationSection = DefaultConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.AddOptions<OllamaProviderOptions>()
            .BindConfiguration(configurationSection);

        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        services.TryAddSingleton(sp =>
            sp.GetRequiredService<ObjectPoolProvider>().Create(new MessageListPooledObjectPolicy()));

        // Per-call API-client factory: each provider-factory CreateModel
        // call resolves its merged per-agent OllamaProviderOptions and
        // hands them to IOllamaApiClientFactory, which news up an
        // HttpClient + OllamaApiClient configured for that endpoint.
        // No HttpClient pooling, no leaky abstractions through DI.
        services.TryAddSingleton<IOllamaApiClientFactory, OllamaApiClientFactory>();

        services.AddSingleton<IProviderFactory, OllamaProviderFactory>();
        services.AddSingleton<IEmbeddingProviderFactory, OllamaEmbeddingProviderFactory>();

        return services;
    }
}
