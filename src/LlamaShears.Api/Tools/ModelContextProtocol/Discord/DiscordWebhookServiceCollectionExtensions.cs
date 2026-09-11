using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Discord;

public static class DiscordWebhookServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordWebhookTools(
        this IServiceCollection services,
        string configurationSection = DiscordWebhookOptions.DefaultConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<DiscordWebhookOptions>, DiscordWebhookOptionsValidator>());
        services.AddOptions<DiscordWebhookOptions>()
            .BindConfiguration(configurationSection)
            .ValidateOnStart();
        services.AddHttpClient(DiscordWebhookOptions.HttpClientName, static client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("LlamaShears");
            })
            .ConfigurePrimaryHttpMessageHandler(static () => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
            });
        return services;
    }
}
