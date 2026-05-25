using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LlamaShears.Core;

namespace LlamaShears.Core.Cron;

public static class CronServiceCollectionExtensions
{
    public static IServiceCollection AddCron(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ICronStore, JsonCronStore>();
        services.TryAddScoped<ICronScheduler, CronScheduler>();
        services.AddAgentService<AgentCronService>();

        return services;
    }
}
