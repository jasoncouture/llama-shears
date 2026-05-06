using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LlamaShears.Hosting;

public static class HostingServiceCollectionExtensions
{
    /// <summary>
    /// Idempotent. Registers the runner that drives
    /// <see cref="IHostStartupTask"/> execution at host startup.
    /// </summary>
    public static IServiceCollection AddHostStartupTaskRunner(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHostedService<HostStartupTaskRunner>();
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TTask"/> as an
    /// <see cref="IHostStartupTask"/>. Multiple registrations of the
    /// same task type are kept; each one runs.
    /// </summary>
    public static IServiceCollection AddHostStartupTask<TTask>(this IServiceCollection services)
        where TTask : class, IHostStartupTask
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<TTask>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IHostStartupTask, TTask>());
        return services;
    }
}
