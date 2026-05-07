using Microsoft.Extensions.DependencyInjection;
using StrangeSoft.Plugins.Host;

namespace LlamaShears.Plugins;

public static class PluginServiceCollectionExtensions
{
    public static IServiceCollectionSnapshot Snapshot(this IServiceCollection services)
    {
        return new ServiceCollectionSnapshot(services);
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

    public static async ValueTask<IServiceCollection> TryApplyPlugins(
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
