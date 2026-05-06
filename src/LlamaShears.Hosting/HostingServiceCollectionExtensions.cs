using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LlamaShears.Hosting;

/// <summary>
/// Dependency-injection helpers for the host startup-task framework.
/// </summary>
public static class HostingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="HostStartupTaskRunner"/> as the
    /// <see cref="IHostedService"/> that drives
    /// <see cref="IHostStartupTask"/> execution at host startup. Idempotent.
    /// </summary>
    public static IServiceCollection AddHostStartupTaskRunner(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, HostStartupTaskRunner>());
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TTask"/> as an
    /// <see cref="IHostStartupTask"/>. Multiple registrations of the
    /// same task type are kept (each one runs).
    /// </summary>
    public static IServiceCollection AddHostStartupTask<TTask>(this IServiceCollection services)
        where TTask : class, IHostStartupTask
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<TTask>();
        services.AddScoped<IHostStartupTask>(sp => sp.GetRequiredService<TTask>());
        return services;
    }
}
