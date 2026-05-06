using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LlamaShears.Hosting;

public static class HostingServiceCollectionExtensions
{
    public const string DefaultShearsPathsConfigurationSection = "Paths";

    public static IServiceCollection AddHostStartupTaskRunner(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHostedService<HostStartupTaskRunner>();
        return services;
    }

    public static IServiceCollection AddHostStartupTask<TTask>(this IServiceCollection services)
        where TTask : class, IHostStartupTask
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHostStartupTaskRunner();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IHostStartupTask, TTask>());
        return services;
    }

    public static IServiceCollection AddShearsPaths(
        this IServiceCollection services,
        string configurationSection = DefaultShearsPathsConfigurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

        services.AddOptions<ShearsPathsOptions>()
            .BindConfiguration(configurationSection);
        services.TryAddSingleton<IShearsPaths, ShearsPaths>();

        return services;
    }
}
