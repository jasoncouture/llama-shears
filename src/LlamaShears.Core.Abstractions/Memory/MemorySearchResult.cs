namespace LlamaShears.Core.Abstractions.Memory;

/// <summary>
/// One hit returned by <see cref="IMemorySearcher.SearchAsync"/>:
/// where the memory lives and how similar it is to the query.
/// </summary>
/// <param name="RelativePath">Workspace-relative path to the memory file.</param>
/// <param name="Score">Cosine-similarity score, in [0, 1].</param>
public sealed record MemorySearchResult(string RelativePath, double Score);
