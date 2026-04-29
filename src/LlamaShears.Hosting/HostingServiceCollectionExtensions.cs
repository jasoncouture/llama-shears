using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LlamaShears.Hosting;

public static class HostingServiceCollectionExtensions
{
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
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IHostStartupTask, TTask>());
        return services;
    }
}
