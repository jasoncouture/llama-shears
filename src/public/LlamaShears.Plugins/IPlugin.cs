using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Plugins;

/// <summary>
/// Contract every plugin exposes. Instances are produced by the
/// assembly's <c>IPluginLoader&lt;IPlugin&gt;</c> implementation and
/// threaded through the host's startup, request pipeline, and lifecycle.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Stable identifier the host uses to disambiguate plugins.
    /// Defaults to the implementing type's simple name; override only
    /// when the host needs to distinguish two plugins that share one.
    /// </summary>
    public string Name => GetType().Name;

    /// <summary>
    /// Register the plugin's services with the host's DI container.
    /// Invoked exactly once during host construction, before any
    /// <see cref="IServiceProvider"/> is built.
    /// </summary>
    public void Register(IServiceCollection services);

    /// <summary>
    /// Asynchronous one-shot initialization, run after the service
    /// provider has been built and before <see cref="Build"/>. Default
    /// is a completed task.
    /// </summary>
    public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Hook into the host's HTTP request pipeline (middleware,
    /// endpoint routing, etc.). Invoked exactly once after every
    /// plugin's <see cref="InitializeAsync"/> has completed. Default
    /// is a no-op so plugins that don't expose HTTP surface can ignore
    /// it.
    /// </summary>
    public void Build(IApplicationBuilder applicationBuilder)
    {
    }

    /// <summary>
    /// Called when the plugin is being unloaded — graceful cleanup of
    /// state the plugin owns outside the DI container. Default is a
    /// completed value task.
    /// </summary>
    public Task UnloadingAsync(IServiceProvider services, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
