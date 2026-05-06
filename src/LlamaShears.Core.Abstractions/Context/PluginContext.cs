using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Frozen view of plugin-attached state at the moment the
/// <see cref="AgentContext"/> snapshot was composed. Keys are the plugin's
/// chosen string identifier; values are plugin-defined records (which
/// must themselves be immutable for the context-tree contract to hold).
/// Plugins write through a scope-specific writer service; this record is
/// read-only.
/// </summary>
public sealed record PluginContext(ImmutableDictionary<string, object> Items);
