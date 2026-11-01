using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Timer;

public static class TimerServiceCollectionExtensions
{
    public static IServiceCollection AddTimerTools(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<TimerScheduler>();
        services.TryAddSingleton<ITimerScheduler>(static serviceProvider => serviceProvider.GetRequiredService<TimerScheduler>());
        services.AddHostedService(static serviceProvider => serviceProvider.GetRequiredService<TimerScheduler>());
        return services;
    }
}
