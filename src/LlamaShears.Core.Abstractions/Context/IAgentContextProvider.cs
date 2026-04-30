namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Composes <see cref="AgentContext"/> snapshots on demand from the
/// host's authoritative sources (config, language model, plugins, etc.).
/// Returns <see langword="null"/> when no context can be built — for the
/// parameterless overload, when there is no ambient agent; for the
/// id-bearing overload, when the agent does not exist.
/// </summary>
public interface IAgentContextProvider
{
    /// <summary>
    /// Builds a snapshot for the ambient agent — the agent whose
    /// execution scope the calling code is running inside. Returns
    /// <see langword="null"/> when there is no current ambient agent.
    /// </summary>
    ValueTask<AgentContext?> CreateAgentContextAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Builds a snapshot for <paramref name="agentId"/>. Returns
    /// <see langword="null"/> when no agent with that id is configured.
    /// </summary>
    ValueTask<AgentContext?> CreateAgentContextAsync(string agentId, CancellationToken cancellationToken);
}
