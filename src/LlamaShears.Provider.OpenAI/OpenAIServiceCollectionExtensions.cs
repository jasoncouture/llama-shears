using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Provider.OpenAI;

public static class OpenAIServiceCollectionExtensions
{
    public const string DefaultConfigurationSection = "Providers:OpenAI";

    public static IServiceCollection AddOpenAIProvider(
        this IServiceCollection services,
        string configurationSection = DefaultConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.AddOptions<OpenAIProviderOptions>()
            .BindConfiguration(configurationSection);

        services.AddHttpClient(nameof(OpenAILanguageModel));

        services.AddSingleton<IProviderFactory, OpenAIProviderFactory>();

        return services;
    }
}
