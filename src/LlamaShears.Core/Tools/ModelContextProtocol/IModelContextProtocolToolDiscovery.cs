using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Context;

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
    /// tools, and returns one <see cref="ToolGroup"/> per server that
    /// responded — the group's <see cref="ToolGroup.Source"/> carries
    /// the server name. Servers that fail (network error, handshake
    /// failure, etc.) are logged and dropped from the result, never
    /// thrown.
    /// </summary>
    ValueTask<ImmutableArray<ToolGroup>> DiscoverAsync(
        IReadOnlyDictionary<string, Uri> servers,
        CancellationToken cancellationToken);
}
