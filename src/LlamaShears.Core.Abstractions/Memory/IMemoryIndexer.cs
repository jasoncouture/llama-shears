namespace LlamaShears.Core.Abstractions.Memory;

/// <summary>
/// Reconciles the agent's memory index against the filesystem.
/// Walks <c>memory/**/*.md</c> and the index together: new files get
/// indexed, changed files re-indexed, and orphaned index entries
/// removed. Invoked explicitly by the <c>memory_index</c> tool and on
/// any future periodic schedule.
/// </summary>
public interface IMemoryIndexer
{
    /// <summary>
    /// Walks the agent's <c>memory/**/*.md</c> tree and the index in
    /// lockstep, applying inserts, updates, and deletions so the index
    /// matches the filesystem. Returns counts for telemetry. When
    /// <paramref name="force"/> is <c>true</c>, every file is re-embedded
    /// regardless of whether its content hash already matches the indexed
    /// hash — use this after changing embedding-model or prompt-decoration
    /// behavior so the existing index can be rebuilt without editing or
    /// re-saving each file.
    /// </summary>
    ValueTask<MemoryReconciliation> ReconcileAsync(string agentId, bool force, CancellationToken cancellationToken);
}
