using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

/// <summary>
/// Routes a model-issued <see cref="ToolCall"/> back to the MCP server
/// that owns the tool, invokes it, and returns the textual result the
/// agent will hand back to the model on the next turn. Failures are
/// captured into the result rather than thrown so the loop can keep
/// going.
/// </summary>
public interface IToolCallDispatcher
{
    /// <summary>
    /// Looks up <paramref name="call"/>'s source in the registry,
    /// opens an MCP session against that server, invokes the named
    /// tool with the supplied arguments, and returns the result.
    /// </summary>
    ValueTask<ToolCallResult> DispatchAsync(ToolCall call, CancellationToken cancellationToken);
}
