using LlamaShears.Plugins;

namespace LlamaShears.Host.Plugins;

/// <summary>
/// Materialized plugin-loading scope: a per-plugin <see cref="System.Runtime.Loader.AssemblyLoadContext"/>
/// already populated with the plugin's root assembly, plus the
/// machinery to discover and instantiate every <see cref="IPluginLoader"/>
/// it contributes.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Discovers <see cref="IPluginLoader"/> implementations within the
    /// context's loaded assemblies, invokes each loader concurrently, and
    /// streams the resulting plugins as they arrive. Loader exceptions are
    /// swallowed and surface as an empty contribution from that loader.
    /// </summary>
    IAsyncEnumerable<IPlugin> LoadPluginsAsync(CancellationToken cancellationToken);
}
