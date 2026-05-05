namespace LlamaShears.Core.Abstractions.Memory;

/// <summary>
/// Vector-search over the agent's memory index. Returns workspace-relative
/// paths and similarity scores; the agent reads bodies on demand via the
/// filesystem read-file tool.
/// </summary>
public interface IMemorySearcher
{
    /// <summary>
    /// Returns the top <paramref name="limit"/> hits whose cosine
    /// similarity to <paramref name="query"/> is at least
    /// <paramref name="minScore"/>, ordered by descending score.
    /// </summary>
    ValueTask<IReadOnlyList<MemorySearchResult>> SearchAsync(
        string agentId,
        string query,
        int limit,
        double minScore,
        CancellationToken cancellationToken);
}
