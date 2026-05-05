using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

/// <summary>
/// Surfaces the agent on whose behalf the current call chain is
/// executing. Backed by <see cref="AsyncLocal{T}"/>, so the value
/// flows across awaits within the same logical call. The
/// loopback-bearer DelegatingHandler reads it on every outbound MCP
/// request to mint the per-call nonce.
/// </summary>
public interface ICurrentAgentAccessor
{
    /// <summary>
    /// The agent currently in scope, or <see langword="null"/> when no
    /// scope has been opened on this call chain.
    /// </summary>
    AgentInfo? Current { get; }

    /// <summary>
    /// Establish the given agent as the current scope until the
    /// returned <see cref="IDisposable"/> is disposed; on dispose, the
    /// previous value (or none) is restored.
    /// </summary>
    IDisposable BeginScope(AgentInfo agent);
}
