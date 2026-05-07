using LlamaShears.Api.Web.Services;
using LlamaShears.Api.Web.Services.SlashCommands;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Api.Web;

public static class WebUiServiceCollectionExtensions
{
    public static IServiceCollection AddWebUi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAgentDirectory, AgentDirectory>();
        services.AddSingleton<IIconProvider, IconProvider>();
        services.AddScoped<IChatSession, ChatSession>();
        services.AddSlashCommands();

        return services;
    }
}
