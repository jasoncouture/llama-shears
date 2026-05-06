using LlamaShears.Provider.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace LlamaShears.Provider.Ollama;

/// <summary>
/// Dependency-injection registration for the Ollama provider.
/// </summary>
public static class OllamaServiceCollectionExtensions
{
    /// <summary>
    /// Default configuration section bound to <see cref="OllamaProviderOptions"/>.
    /// </summary>
    public const string DefaultConfigurationSection = "Providers:Ollama";

    /// <summary>
    /// Registers the Ollama provider. <see cref="OllamaProviderOptions"/> are
    /// bound from <paramref name="configurationSection"/> on the
    /// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> resolved
    /// from DI. <see cref="IOllamaApiClient"/> is registered as a typed
    /// <see cref="HttpClient"/>, so its underlying handler is pooled and
    /// recycled by <see cref="IHttpClientFactory"/>.
    /// </summary>
    public static IServiceCollection AddOllamaProvider(
        this IServiceCollection services,
        string configurationSection = DefaultConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.AddOptions<OllamaProviderOptions>()
            .BindConfiguration(configurationSection);

        services.AddHttpClient<IOllamaApiClient, OllamaApiClient>((sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaProviderOptions>>().Value;
            httpClient.BaseAddress = options.BaseUri;
        });

        services.AddSingleton<IProviderFactory, OllamaProviderFactory>();

        return services;
    }
}
