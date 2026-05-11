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

    /// <summary>
    /// Returns the raw JSON text of the agent's config file plus a hash
    /// of the bytes, or <see langword="null"/> when no file exists.
    /// The hash is the change token <see cref="SaveAsync"/> validates
    /// against; pair this call with a later save to detect concurrent
    /// edits to the same file.
    /// </summary>
    ValueTask<AgentConfigFile?> ReadFileAsync(string agentId, CancellationToken cancellationToken);

    /// <summary>
    /// Writes <paramref name="content"/> to the agent's config file if
    /// the current on-disk hash equals <paramref name="expectedHash"/>
    /// (case-insensitive) and the content deserializes to an
    /// <see cref="AgentConfig"/>. Returns the outcome:
    /// <see cref="SaveAgentConfigResult.Ok"/> on success,
    /// <see cref="SaveAgentConfigResult.Conflict"/> when the hash
    /// doesn't match, or <see cref="SaveAgentConfigResult.InvalidJson"/>
    /// when the content fails validation.
    /// </summary>
    ValueTask<SaveAgentConfigResult> SaveAsync(
        string agentId,
        string expectedHash,
        string content,
        CancellationToken cancellationToken);
}
