namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Source of truth for agent configuration. Reads from the configured
/// agents directory (<c>&lt;Data&gt;/agents/&lt;id&gt;.json</c>) and is
/// the single read API for both "what agents exist" and "what's the
/// config for this agent". Implementations may cache by file metadata
/// but must reflect on-disk edits without a host restart.
/// </summary>
public interface IAgentConfigProvider
{
    /// <summary>
    /// Returns the agent ids currently configured on disk, in stable
    /// lexicographic order.
    /// </summary>
    IReadOnlyList<string> ListAgentIds();

    /// <summary>
    /// Returns the parsed <see cref="AgentConfig"/> for
    /// <paramref name="agentId"/>, or <see langword="null"/> if no
    /// config file exists for that id or the existing file fails to
    /// parse.
    /// </summary>
    ValueTask<AgentConfig?> GetConfigAsync(string agentId, CancellationToken cancellationToken);
}
