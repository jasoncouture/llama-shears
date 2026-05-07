using System.Collections.Immutable;

namespace LlamaShears.Plugins.Abstractions;

/// <summary>
/// Assembly-level entry point a plugin assembly exposes for a host
/// expecting plugins of type <typeparamref name="T"/>. The host loader
/// instantiates this via parameterless constructor and calls
/// <see cref="LoadAsync"/> to materialize the plugin instances.
/// </summary>
/// <typeparam name="T">
/// The host's plugin contract. The generic SDK has no opinion on its
/// shape — the host defines it (interface, base class, anything).
/// </typeparam>
public interface IPluginLoader<T> where T : class
{
    /// <summary>
    /// Returns the plugins this assembly contributes. An empty array
    /// is the canonical "this assembly chose not to activate" signal
    /// (feature flag off, host runtime mismatch, etc.); the host
    /// treats it as a successful no-op rather than an error.
    /// </summary>
    public Task<ImmutableArray<T>> LoadAsync(CancellationToken cancellationToken);
}
