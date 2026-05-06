using System.Collections.Immutable;

namespace LlamaShears.Core.Plugin.Core;

/// <summary>
/// Assembly-level entry point for plugin discovery. A plugin assembly
/// declares one type implementing this contract; the host's loader
/// instantiates it (parameterless ctor) and calls <see cref="LoadAsync"/>
/// to obtain the plugins the assembly elects to activate.
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Returns the plugins this assembly contributes. An empty array
    /// is the canonical "this assembly chose not to activate" signal
    /// (feature flag off, host runtime mismatch, etc.); the host
    /// treats it as a successful no-op rather than an error.
    /// </summary>
    public ValueTask<ImmutableArray<IPlugin>> LoadAsync(CancellationToken cancellationToken);
}
