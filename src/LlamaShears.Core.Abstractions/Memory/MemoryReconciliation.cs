namespace LlamaShears.Core.Abstractions.Memory;

/// <summary>
/// Counts returned by <see cref="IMemoryIndexer.ReconcileAsync"/>. Pure
/// telemetry — the source of truth for memory content is the
/// filesystem, the index is derivative.
/// </summary>
/// <param name="Added">Files newly indexed during this pass.</param>
/// <param name="Updated">Files re-indexed because their content hash changed (or <c>force</c> was set).</param>
/// <param name="Removed">Index entries deleted because the underlying file no longer exists.</param>
/// <param name="Total">Total files observed under the agent's <c>memory/</c> tree.</param>
public sealed record MemoryReconciliation(int Added, int Updated, int Removed, int Total);
