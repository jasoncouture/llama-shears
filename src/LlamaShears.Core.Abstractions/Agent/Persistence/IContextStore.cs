using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent.Persistence;

/// <summary>
/// Storage seam for per-agent conversation logs. Implementations decide
/// the backing format (today: append-only JSON-line files on disk) and
/// expose live <see cref="IAgentContext"/> views plus archive-reading,
/// listing, and clearing operations.
/// </summary>
public interface IContextStore
{
    /// <summary>
    /// Opens the live, mutable context for <paramref name="agentId"/>,
    /// loading any persisted entries on first open. Repeated calls for
    /// the same agent return the same instance for the lifetime of the
    /// store.
    /// </summary>
    Task<IAgentContext> OpenAsync(string agentId, CancellationToken cancellationToken);

    /// <summary>
    /// Streams the persisted entries from the agent's current (active)
    /// context file in arrival order. Does not affect any open
    /// <see cref="IAgentContext"/>.
    /// </summary>
    IAsyncEnumerable<IContextEntry> ReadCurrentAsync(string agentId, CancellationToken cancellationToken);

    /// <summary>
    /// Streams the persisted entries from a specific archived context
    /// file identified by <paramref name="archiveId"/>.
    /// </summary>
    IAsyncEnumerable<IContextEntry> ReadArchiveAsync(ArchiveId archiveId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the ids of every agent that has any persisted state in
    /// this store, in stable lexicographic order.
    /// </summary>
    Task<IReadOnlyList<string>> ListAgentsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns every archive id stored for <paramref name="agentId"/>
    /// in chronological order (oldest first). The agent's current,
    /// non-archived context is not included.
    /// </summary>
    Task<IReadOnlyList<ArchiveId>> ListArchivesAsync(string agentId, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the agent's stored context. With <paramref name="archive"/>=true,
    /// renames <c>current.json</c> to <c>&lt;UnixMillis&gt;.json</c>; otherwise
    /// deletes <c>current.json</c>. The agent's folder is never removed by the
    /// framework — that is the user's or a plugin's job.
    /// </summary>
    Task ClearAsync(string agentId, bool archive, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a single archive file identified by <paramref name="archiveId"/>.
    /// Does not touch <c>current.json</c>, other archives, or the agent folder.
    /// </summary>
    Task DeleteAsync(ArchiveId archiveId, CancellationToken cancellationToken);
}
