using System.Collections.Immutable;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

/// <summary>
/// Single source of truth for the MCP servers known to the host.
/// Combines the central name→config map (configured via
/// <see cref="ModelContextProtocolOptions"/>) with the host's own
/// internal MCP endpoint, then resolves an agent's whitelist into the
/// concrete server map handed to discovery.
/// </summary>
public interface IModelContextProtocolServerRegistry
{
    /// <summary>
    /// Returns the name→config map for the supplied whitelist. A
    /// <see langword="null"/> whitelist means "all known servers";
    /// any other value is an exact filter — names not present in the
    /// registry are dropped (and logged).
    /// </summary>
    IReadOnlyDictionary<string, ModelContextProtocolServerOptions> Resolve(ImmutableHashSet<string>? whitelist);

    /// <summary>
    /// Looks up a single server by name. Returns <see langword="null"/>
    /// when the name is unknown. Used by transport-construction paths
    /// that have only a server name on hand (e.g. routing handlers).
    /// </summary>
    ModelContextProtocolServerOptions? TryGet(string name);
}
