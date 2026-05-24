namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Gatekeeps which MCP tools the agent sees during tool discovery. Applied
/// inside the discovery pipeline so blocked tools never reach the language
/// model's tool list — the model cannot select what it cannot see.
/// Implementations can short-circuit at three levels: all tools, all tools
/// from a given source, or a specific tool by name.
/// </summary>
public interface IToolFilter
{
    /// <summary>
    /// Indicates whether any tools should be exposed in the current context.
    /// Returning <see langword="false"/> short-circuits discovery and yields an empty tool list.
    /// </summary>
    /// <returns><see langword="true"/> when tools should be exposed; otherwise <see langword="false"/>.</returns>
    bool AreToolsAllowed();

    /// <summary>
    /// Indicates whether any tool from the given MCP source should be exposed.
    /// Sources are the MCP server names the agent is configured to consume
    /// (for example <c>llamashears</c> for the internal server, or an external server name).
    /// </summary>
    /// <param name="source">MCP source / server name.</param>
    /// <returns><see langword="true"/> when tools from the source should be exposed; otherwise <see langword="false"/>.</returns>
    bool IsSourceAllowed(string source);

    /// <summary>
    /// Indicates whether a specific tool from a given source should be exposed.
    /// Invoked once per discovered tool after <see cref="AreToolsAllowed"/> and
    /// <see cref="IsSourceAllowed"/> have both returned <see langword="true"/>.
    /// </summary>
    /// <param name="source">MCP source / server name.</param>
    /// <param name="toolName">The tool's declared name as returned by <c>list_tools</c>.</param>
    /// <returns><see langword="true"/> when the named tool should be exposed; otherwise <see langword="false"/>.</returns>
    bool IsToolAllowed(string source, string toolName);
}
