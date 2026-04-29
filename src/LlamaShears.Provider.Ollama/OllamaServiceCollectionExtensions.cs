using LlamaShears.Provider.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OllamaSharp;

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

        services
            .AddHttpClient(nameof(OllamaApiClient), (sp, httpClient) =>
            {
                var options = sp.GetRequiredService<IOptions<OllamaProviderOptions>>().Value;
                httpClient.BaseAddress = options.BaseUri;
            })
            .AddTypedClient<IOllamaApiClient>(httpClient => new OllamaApiClient(httpClient));

        services.AddSingleton<IProviderFactory, OllamaProviderFactory>();

        return services;
    }
}
