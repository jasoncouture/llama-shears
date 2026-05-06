using System.Collections.Immutable;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

/// <summary>
/// Connects to MCP servers an agent has configured and surfaces their
/// available tools. Network failures are swallowed and logged so a
/// single bad server cannot prevent discovery from returning the rest.
/// </summary>
public interface IModelContextProtocolToolDiscovery
{
    /// <summary>
    /// Connects to each MCP server in the supplied map, lists its
    /// tools, and returns one <see cref="ModelContextProtocolToolSet"/>
    /// per server that responded. Servers that fail (network error,
    /// handshake failure, etc.) are logged and dropped from the result,
    /// never thrown.
    /// </summary>
    ValueTask<ImmutableArray<ModelContextProtocolToolSet>> DiscoverAsync(
        IReadOnlyDictionary<string, Uri> servers,
        CancellationToken cancellationToken);
}
