using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Free-form key/value bag plugins use to surface state on an
/// <see cref="AgentContext"/> snapshot. Keys are namespaced by the
/// owning plugin to avoid collisions; the framework treats values as
/// opaque.
/// </summary>
/// <param name="Data">The plugin keyspace for this snapshot.</param>
public sealed record PluginContext(ImmutableDictionary<string, object> Data);
