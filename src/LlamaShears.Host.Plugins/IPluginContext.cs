namespace LlamaShears.Host.Plugins;

/// <summary>
/// Materialized plugin-loading scope: a per-plugin
/// <see cref="System.Runtime.Loader.AssemblyLoadContext"/> already
/// populated with the plugin's root assembly, plus the machinery to
/// discover and instantiate every <c>IPluginLoader&lt;T&gt;</c> it
/// contributes.
/// </summary>
/// <typeparam name="T">
/// The host's plugin contract. The discovery walk looks for types
/// implementing <c>StrangeSoft.Plugins.Abstractions.IPluginLoader&lt;T&gt;</c>
/// for this exact T.
/// </typeparam>
public interface IPluginContext<T> where T : class
{
    /// <summary>
    /// Discovers loader types within the context's loaded assemblies,
    /// invokes each loader concurrently, and streams the resulting
    /// plugins as they arrive. Loader exceptions are swallowed and
    /// surface as an empty contribution from that loader.
    /// </summary>
    IAsyncEnumerable<T> LoadPluginsAsync(CancellationToken cancellationToken);
}
