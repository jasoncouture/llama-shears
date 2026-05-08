namespace LlamaShears.Core.Abstractions.Memory;

/// <summary>
/// One hit returned by <see cref="IMemorySearcher.SearchAsync"/>:
/// where the memory lives, how similar it is to the query, the
/// first line as a one-shot summary, and the full body. Both the
/// summary and the body come from a single cached file read so
/// callers don't need to re-open the file.
/// </summary>
/// <param name="RelativePath">Workspace-relative path to the memory file.</param>
/// <param name="Score">Cosine-similarity score, in [0, 1].</param>
/// <param name="Summary">First line of the backing file (typically a markdown H1). Empty when the file has no content.</param>
/// <param name="Content">Full file body. Empty when the file is empty.</param>
public sealed record MemorySearchResult(string RelativePath, double Score, string Summary, string Content);
