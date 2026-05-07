using System.Reflection;
using System.Runtime.Loader;

namespace StrangeSoft.Plugins.Host;

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
    /// The underlying <see cref="System.Runtime.Loader.AssemblyLoadContext"/>
    /// the plugin is loaded into. Hosts that need lower-level access
    /// (e.g., to subscribe to the context's own events, walk loaded
    /// assemblies, or trigger collection on a collectible context) can
    /// reach the ALC directly through this property.
    /// </summary>
    AssemblyLoadContext AssemblyLoadContext { get; }

    /// <summary>
    /// Appends an additional <see cref="IAssemblyResolver"/> to the
    /// chain consulted when the runtime fails to resolve an assembly
    /// referenced by code in this context. Resolvers are walked in
    /// the order they were added; the first non-null result wins.
    /// </summary>
    void AddAssemblyResolver(IAssemblyResolver resolver);

    /// <summary>
    /// Loads an assembly into this context by identity. Equivalent to
    /// <see cref="AssemblyLoadContext.LoadFromAssemblyName(AssemblyName)"/>
    /// on <see cref="AssemblyLoadContext"/>.
    /// </summary>
    Assembly LoadFromAssemblyName(AssemblyName assemblyName);

    /// <summary>
    /// Loads an assembly into this context from a file path. Equivalent
    /// to <see cref="AssemblyLoadContext.LoadFromAssemblyPath(string)"/>
    /// on <see cref="AssemblyLoadContext"/>.
    /// </summary>
    Assembly LoadFromAssemblyPath(string assemblyPath);

    /// <summary>
    /// Forwarded <c>Resolving</c> event from the underlying
    /// <see cref="AssemblyLoadContext"/>. Subscribe here to react when
    /// resolution falls through every <see cref="IAssemblyResolver"/>
    /// added to the chain. Returning a non-null assembly satisfies
    /// the binding; returning <see langword="null"/> lets resolution
    /// continue to fail.
    /// </summary>
    event Func<AssemblyLoadContext, AssemblyName, Assembly?> Resolving;

    /// <summary>
    /// Discovers loader types within the context's loaded assemblies,
    /// invokes each loader concurrently, and streams the resulting
    /// plugins as they arrive. Loader exceptions are swallowed and
    /// surface as an empty contribution from that loader.
    /// </summary>
    IAsyncEnumerable<T> LoadPluginsAsync(CancellationToken cancellationToken);
}
