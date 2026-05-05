namespace LlamaShears.Core.Tools.ModelContextProtocol;

/// <summary>
/// Surfaces the runtime URI of the host's own MCP endpoint so the
/// agent loader can inject it into every agent's server map. Framework-
/// internal: not part of the public plugin contract, hence outside
/// <c>*.Abstractions</c>.
/// </summary>
public interface IInternalModelContextProtocolServer
{
    /// <summary>
    /// Absolute URI of the host's MCP endpoint, or <see langword="null"/>
    /// when it cannot be determined yet (e.g. the listener has not bound
    /// any addresses). Callers must handle the null case as "not
    /// available right now"; a subsequent call may return a real URI
    /// once the host has finished starting up.
    /// </summary>
    Uri? Uri { get; }
}
