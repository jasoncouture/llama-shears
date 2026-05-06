namespace LlamaShears.Core.Abstractions.Memory;

/// <summary>
/// Writes a new memory file under the agent's workspace. Storage is the
/// source of truth; the embedding index is derivative and self-heals on
/// retrieval and on full reconcile (see <see cref="IMemoryIndexer"/>).
/// </summary>
public interface IMemoryStore
{
    /// <summary>
    /// Persists <paramref name="content"/> under
    /// <c>memory/YYYY-MM-DD/&lt;unix-seconds&gt;.md</c> in the agent's
    /// workspace and triggers eager indexing. Indexing failures are
    /// logged and swallowed — the file is still written, and the next
    /// <see cref="IMemoryIndexer.ReconcileAsync"/> picks it up.
    /// </summary>
    ValueTask<MemoryRef> StoreAsync(string agentId, string content, CancellationToken cancellationToken);
}
