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
    /// Registers the Ollama provider, its <see cref="IOllamaApiClient"/>, and
    /// <see cref="OllamaProviderOptions"/> with the given service collection.
    /// </summary>
    public static IServiceCollection AddOllamaProvider(
        this IServiceCollection services,
        Action<OllamaProviderOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<OllamaProviderOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.AddSingleton<IOllamaApiClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaProviderOptions>>().Value;
            return new OllamaApiClient(options.BaseUri);
        });

        services.AddSingleton<IProviderFactory, OllamaProviderFactory>();

        return services;
    }
}
