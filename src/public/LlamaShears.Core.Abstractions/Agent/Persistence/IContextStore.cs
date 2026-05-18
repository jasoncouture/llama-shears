using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent.Persistence;

/// <summary>
/// Storage seam for per-agent conversation logs. Implementations decide
/// the backing format (today: append-only JSON-line files on disk) and
/// expose live <see cref="IAgentContext"/> views plus archive-reading,
/// listing, and clearing operations.
/// </summary>
/// <remarks>
/// Methods take a <c>sessionId</c> dimension; the default session is
/// identified by <see cref="Guid.Empty"/> and resolves to the agent's
/// root storage layout (backward-compatible). Non-default sessions
/// persist under a per-session subfolder. The store does not auto-load
/// or otherwise lifecycle-manage non-default sessions — they exist
/// only when something explicitly opens them.
/// </remarks>
public interface IContextStore
{
    /// <summary>
    /// Opens the live, mutable context for
    /// (<paramref name="agentId"/>, <paramref name="sessionId"/>),
    /// loading any persisted entries on first open. Repeated calls for
    /// the same pair return the same instance for the lifetime of the
    /// store.
    /// </summary>
    Task<IAgentContext> OpenAsync(string agentId, Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Convenience overload that targets the agent's default session
    /// (<see cref="Guid.Empty"/>).
    /// </summary>
    Task<IAgentContext> OpenAsync(string agentId, CancellationToken cancellationToken)
        => OpenAsync(agentId, Guid.Empty, cancellationToken);

    /// <summary>
    /// Streams the persisted entries from the session's current (active)
    /// context file in arrival order. Does not affect any open
    /// <see cref="IAgentContext"/>.
    /// </summary>
    IAsyncEnumerable<IContextEntry> ReadCurrentAsync(string agentId, Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Convenience overload that targets the agent's default session
    /// (<see cref="Guid.Empty"/>).
    /// </summary>
    IAsyncEnumerable<IContextEntry> ReadCurrentAsync(string agentId, CancellationToken cancellationToken)
        => ReadCurrentAsync(agentId, Guid.Empty, cancellationToken);

    /// <summary>
    /// Streams the persisted entries from a specific archived context
    /// file identified by <paramref name="archiveId"/>.
    /// </summary>
    IAsyncEnumerable<IContextEntry> ReadArchiveAsync(ArchiveId archiveId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the ids of every agent that has any persisted state in
    /// this store, in stable lexicographic order. GUID-named subfolders
    /// (non-default session storage) are not surfaced as agents.
    /// </summary>
    Task<IReadOnlyList<string>> ListAgentsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns every archive id stored for
    /// (<paramref name="agentId"/>, <paramref name="sessionId"/>)
    /// in chronological order (oldest first). The session's current,
    /// non-archived context is not included.
    /// </summary>
    Task<IReadOnlyList<ArchiveId>> ListArchivesAsync(string agentId, Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Convenience overload that targets the agent's default session
    /// (<see cref="Guid.Empty"/>).
    /// </summary>
    Task<IReadOnlyList<ArchiveId>> ListArchivesAsync(string agentId, CancellationToken cancellationToken)
        => ListArchivesAsync(agentId, Guid.Empty, cancellationToken);

    /// <summary>
    /// Clears the session's stored context. With
    /// <paramref name="archive"/>=true, renames <c>current.json</c> to
    /// <c>&lt;UnixMillis&gt;.json</c>; otherwise deletes
    /// <c>current.json</c>. The agent or session folder is never removed
    /// by the framework — that is the user's or a plugin's job.
    /// </summary>
    Task ClearAsync(string agentId, Guid sessionId, bool archive, CancellationToken cancellationToken);

    /// <summary>
    /// Convenience overload that targets the agent's default session
    /// (<see cref="Guid.Empty"/>).
    /// </summary>
    Task ClearAsync(string agentId, bool archive, CancellationToken cancellationToken)
        => ClearAsync(agentId, Guid.Empty, archive, cancellationToken);

    /// <summary>
    /// Deletes a single archive file identified by <paramref name="archiveId"/>.
    /// Does not touch <c>current.json</c>, other archives, or the agent folder.
    /// </summary>
    Task DeleteAsync(ArchiveId archiveId, CancellationToken cancellationToken);
}
