namespace LlamaShears.Core.Tools.ModelContextProtocol;

/// <summary>
/// Common shape for MCP tool responses serialized as JSON. Implementations expose
/// tool-specific fields alongside a shared optional error channel so callers can
/// distinguish success payloads from failure messages without parsing free-form text.
/// </summary>
public interface IToolResponse
{
    /// <summary>
    /// Human-readable error message when the tool call could not produce a normal result;
    /// <c>null</c> on success. When set, sibling payload fields should be treated as empty
    /// or sentinel values.
    /// </summary>
    string? Error { get; }
}
