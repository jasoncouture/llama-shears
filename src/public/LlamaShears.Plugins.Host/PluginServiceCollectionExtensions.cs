using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StrangeSoft.Plugins.Host;

namespace LlamaShears.Plugins.Host;

public static class PluginServiceCollectionExtensions
{
    public static IServiceCollectionSnapshot Snapshot(this IServiceCollection services)
    {
        return new ServiceCollectionSnapshot(services);
    }

    internal class LoggingStartupHostedService : IHostedService
    {
        public LoggingStartupHostedService(ILoggerFactory loggerFactory, DeferredPluginHostLogger deferredPluginHostLogger)
        {
            deferredPluginHostLogger.RedirectTo(loggerFactory.CreateLogger("LlamaShears.Plugins"));
        }
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public static IServiceCollection AddPluginDefferedLogger(this IServiceCollection services, Assembly? assembly)
    {
        var logger = new DeferredPluginHostLogger();
        services.AddSingleton(logger);
        DefaultPluginContextLogger.Instance = logger;
        services.AddHostedService<LoggingStartupHostedService>();
        HostContextAssemblyResolver.TryInitialize(TryLocateAssembly(assembly));

        return services;
    }

    private static Assembly? TryLocateAssembly(Assembly? assembly)
    {
        return assembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
    }

    public static async ValueTask UsePluginsAsync(this IApplicationBuilder applicationBuilder, CancellationToken cancellationToken)
    {
        var plugins = applicationBuilder.ApplicationServices.GetServices<IPlugin>();

        foreach (var plugin in plugins)
        {
            await plugin.InitializeAsync(applicationBuilder.ApplicationServices, cancellationToken).WaitAsync(cancellationToken);
        }

        foreach (var plugin in plugins)
        {
            plugin.Build(applicationBuilder);
        }
    }

    public static async Task<IServiceCollection> LoadPluginsAsync(this IServiceCollection services, Func<IPlugin, Exception, bool>? failureCallback,
        CancellationToken cancellationToken, params IEnumerable<IPluginLocator<IPlugin>> pluginLocators)
    {
        services.AddPluginDefferedLogger(Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly());

        foreach (var plugin in Plugin.LoadPluginContexts(pluginLocators))
        {
            await services.TryApplyPluginsAsync(plugin, failureCallback, cancellationToken);
        }

        return services;
    }

    public static IServiceCollection TryApplyPlugin(
        this IServiceCollection services,
        IPlugin plugin,
        Func<IPlugin, Exception, bool>? failureCallback)
    {
        using var snapshot = services.Snapshot();
        try
        {
            plugin.Register(services);
            services.AddSingleton(plugin);
            snapshot.AcceptChanges();
        }
        catch (Exception ex)
        {
            if (failureCallback?.Invoke(plugin, ex) != true)
            {
                throw;
            }
        }
        return services;
    }

    public static async ValueTask<IServiceCollection> TryApplyPluginsAsync(
        this IServiceCollection services,
        IPluginContext<IPlugin> context,
        Func<IPlugin, Exception, bool>? failureCallback,
        CancellationToken cancellationToken)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            await foreach (var plugin in context.LoadPluginsAsync(cancellationTokenSource.Token))
            {
                services.TryApplyPlugin(plugin, failureCallback);
                cancellationTokenSource.Token.ThrowIfCancellationRequested();
            }
        }
        finally
        {
            if (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                await cancellationTokenSource.CancelAsync();
            }
        }
        return services;
    }
}
