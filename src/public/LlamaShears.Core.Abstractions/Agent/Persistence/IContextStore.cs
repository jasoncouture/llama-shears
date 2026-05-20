using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent.Persistence;

/// <summary>
/// Storage seam for per-agent conversation logs. Implementations decide
/// the backing format (today: append-only JSON-line files on disk) and
/// expose live <see cref="IAgentContext"/> views plus archive-reading,
/// listing, and clearing operations.
/// </summary>
/// <remarks>
/// The session dimension rides on <see cref="SessionId"/>. The default
/// session is identified by <see cref="SessionId.IsDefault"/> and
/// resolves to the agent's root storage layout; non-default sessions
/// persist under <c>&lt;agentId&gt;/&lt;sessionName&gt;/</c> with their
/// current file named <c>&lt;sessionId:n&gt;.json</c>.
/// </remarks>
public interface IContextStore
{
    /// <summary>
    /// Opens the live, mutable context for <paramref name="session"/>,
    /// loading any persisted entries on first open. Repeated calls for
    /// the same session return the same instance for the lifetime of the
    /// store.
    /// </summary>
    Task<IAgentContext> OpenAsync(SessionId session, CancellationToken cancellationToken);

    /// <summary>
    /// Streams the persisted entries from the session's current (active)
    /// context file in arrival order. Does not affect any open
    /// <see cref="IAgentContext"/>.
    /// </summary>
    IAsyncEnumerable<IContextEntry> ReadCurrentAsync(SessionId session, CancellationToken cancellationToken);

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
    /// Returns every archive id stored for <paramref name="session"/> in
    /// chronological order (oldest first). The session's current,
    /// non-archived context is not included.
    /// </summary>
    Task<IReadOnlyList<ArchiveId>> ListArchivesAsync(SessionId session, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the session's stored context. With
    /// <paramref name="archive"/>=true, renames the session's current
    /// file to <c>&lt;UnixMillis&gt;.json</c>; otherwise deletes it. The
    /// agent or session folder is never removed by the framework — that
    /// is the user's or a plugin's job.
    /// </summary>
    Task ClearAsync(SessionId session, bool archive, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a single archive file identified by <paramref name="archiveId"/>.
    /// Does not touch the session's current file, other archives, or the
    /// agent folder.
    /// </summary>
    Task DeleteAsync(ArchiveId archiveId, CancellationToken cancellationToken);
}
