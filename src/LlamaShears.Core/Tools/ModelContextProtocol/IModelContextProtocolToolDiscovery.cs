using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

/// <summary>
/// Connects to the supplied MCP servers and surfaces their available
/// tools. Network failures are swallowed and logged so a single bad
/// server cannot prevent discovery from returning the rest.
/// </summary>
public interface IModelContextProtocolToolDiscovery
{
    /// <summary>
    /// Lists tools for each name in <paramref name="serverNames"/> via
    /// <see cref="IModelContextProtocolClient"/>. Servers that fail
    /// (network error, handshake failure, etc.) are logged and dropped
    /// from the result.
    /// </summary>
    ValueTask<ImmutableArray<ToolGroup>> DiscoverAsync(
        IEnumerable<string> serverNames,
        CancellationToken cancellationToken);
}
