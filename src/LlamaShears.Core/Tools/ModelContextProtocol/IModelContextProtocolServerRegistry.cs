using System.Collections.Immutable;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

/// <summary>
/// Single source of truth for the MCP servers known to the host.
/// Combines the central name→URI map (configured via
/// <see cref="ModelContextProtocolOptions"/>) with the host's own
/// internal MCP endpoint, then resolves an agent's whitelist into the
/// concrete server map handed to discovery.
/// </summary>
public interface IModelContextProtocolServerRegistry
{
    /// <summary>
    /// Returns the name→URI map for the supplied whitelist. A
    /// <see langword="null"/> whitelist means "all known servers";
    /// any other value is an exact filter — names not present in the
    /// registry are dropped (and logged).
    /// </summary>
    IReadOnlyDictionary<string, Uri> Resolve(ImmutableHashSet<string>? whitelist);
}
