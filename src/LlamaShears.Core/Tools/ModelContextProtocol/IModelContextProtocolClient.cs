using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

/// <summary>
/// Typed MCP client. Callers address a server by its registered name;
/// transport-layer routing rewrites the request to the configured
/// endpoint and stamps any configured headers before the request
/// leaves the host. Pools the underlying <c>McpClient</c> instances
/// per server name so the JSON-RPC <c>initialize</c> handshake is
/// paid once per connection lifetime, not per call.
/// </summary>
/// <remarks>
/// The client deliberately does not own error policy: every failure
/// (transport, protocol, server-not-registered, unknown tool, etc.)
/// is surfaced as an exception. Consumers translate exceptions to
/// their domain-appropriate shape — <see cref="ToolCallResult"/> with
/// <see cref="ToolCallResult.IsError"/> set, log-and-skip, etc.
/// </remarks>
public interface IModelContextProtocolClient
{
    /// <summary>
    /// Lists the tools advertised by the named server. Throws on any
    /// failure (unknown server name, transport error, protocol error).
    /// </summary>
    ValueTask<ImmutableArray<ToolDescriptor>> ListToolsAsync(
        string serverName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Invokes <paramref name="toolName"/> on the named server with
    /// the supplied arguments and returns the server's response.
    /// Throws on any failure; the SDK's <c>IsError</c> flag is
    /// surfaced through the returned <see cref="ToolCallResult"/>.
    /// </summary>
    ValueTask<ToolCallResult> CallToolAsync(
        string serverName,
        string toolName,
        IReadOnlyDictionary<string, object?>? arguments,
        CancellationToken cancellationToken);
}
