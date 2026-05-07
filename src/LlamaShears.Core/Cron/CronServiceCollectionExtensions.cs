using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LlamaShears.Core.Cron;

public static class CronServiceCollectionExtensions
{
    public const string DefaultConfigurationSection = "Cron";

    public static IServiceCollection AddCron(
        this IServiceCollection services,
        string configurationSection = DefaultConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.AddOptions<CronOptions>()
            .BindConfiguration(configurationSection)
            .Validate(o => o.TickInterval > TimeSpan.Zero, "Cron:TickInterval must be strictly positive.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.FileName), "Cron:FileName must not be blank.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ICronStore, JsonCronStore>();
        services.TryAddSingleton<ICronScheduler, CronScheduler>();
        services.AddHostedService<CronExecutor>();

        return services;
    }
}
