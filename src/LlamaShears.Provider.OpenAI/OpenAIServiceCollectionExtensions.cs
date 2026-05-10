using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Provider.OpenAI;

public static class OpenAiServiceCollectionExtensions
{
    public const string DefaultConfigurationSection = "Providers:OpenAI";

    public static IServiceCollection AddOpenAiProvider(
        this IServiceCollection services,
        string configurationSection = DefaultConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.AddOptions<OpenAiProviderOptions>()
            .BindConfiguration(configurationSection);

        services.AddHttpClient(nameof(OpenAiLanguageModel));

        services.AddSingleton<IProviderFactory, OpenAiProviderFactory>();

        return services;
    }
}
