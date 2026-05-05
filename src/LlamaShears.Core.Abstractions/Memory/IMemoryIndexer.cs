namespace LlamaShears.Core.Abstractions.Memory;

/// <summary>
/// Reconciles the agent's memory index against the filesystem.
/// Walks <c>memory/**/*.md</c> and the index together: new files get
/// indexed, changed files re-indexed, and orphaned index entries
/// removed. Invoked explicitly by the <c>index_memory</c> tool and on
/// any future periodic schedule.
/// </summary>
public interface IMemoryIndexer
{
    /// <summary>
    /// Walks the agent's <c>memory/**/*.md</c> tree and the index in
    /// lockstep, applying inserts, updates, and deletions so the index
    /// matches the filesystem. Returns counts for telemetry.
    /// </summary>
    ValueTask<MemoryReconciliation> ReconcileAsync(string agentId, CancellationToken cancellationToken);
}
