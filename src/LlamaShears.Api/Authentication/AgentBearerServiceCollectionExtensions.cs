using LlamaShears.Agent.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Api.Authentication;

public static class AgentBearerServiceCollectionExtensions
{
    public static IServiceCollection AddAgentBearerAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAgentTokenStore();
        services.TryAddSingleton<IAgentClaimsProjector, DefaultAgentClaimsProjector>();

        services.AddAuthentication(AgentBearerDefaults.AuthenticationScheme)
            .AddScheme<AgentBearerOptions, AgentBearerHandler>(
                AgentBearerDefaults.AuthenticationScheme,
                configureOptions: null);

        services.AddAuthorization();

        return services;
    }
}
