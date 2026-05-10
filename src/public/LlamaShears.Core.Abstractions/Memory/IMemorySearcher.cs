namespace LlamaShears.Core.Abstractions.Memory;

/// <summary>
/// Vector-search over the agent's memory index. Returns workspace-relative
/// paths and similarity scores; the agent reads bodies on demand via the
/// filesystem read-file tool.
/// </summary>
public interface IMemorySearcher
{
    /// <summary>
    /// Returns the top hits whose cosine similarity to
    /// <paramref name="query"/> meets the score floor, ordered by
    /// descending score. <paramref name="limit"/> and
    /// <paramref name="minScore"/> default to the agent's
    /// <c>AgentMemoryConfig</c> values when left <see langword="null"/>;
    /// callers (e.g. the memory tool) pass explicit overrides when
    /// they need different bounds.
    /// </summary>
    ValueTask<IReadOnlyList<MemorySearchResult>> SearchAsync(
        string agentId,
        string query,
        int? limit,
        double? minScore,
        CancellationToken cancellationToken);
}
