using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Core.Cron;

public static class CronServiceCollectionExtensions
{
    public static IServiceCollection AddCron(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ICronStore, JsonCronStore>();
        services.TryAddSingleton<ICronScheduler, CronScheduler>();
        services.TryAddActivatedSingleton<CronExecutor>();

        return services;
    }
}
