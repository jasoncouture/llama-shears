namespace LlamaShears.Core.Abstractions.SystemPrompt;

/// <summary>
/// Resolves a template file across the standard layered lookup:
/// per-workspace customization first, then operator-supplied templates,
/// then the bundled defaults that ship with the host. Returns the full
/// path of the first file that exists, or <see langword="null"/> if no
/// candidate hits.
/// </summary>
public interface ITemplateFileLocator
{
    /// <summary>
    /// Locate <paramref name="fileName"/> (e.g. <c>"COMPACTION.md"</c>)
    /// inside the optional <paramref name="subFolder"/>; on miss, fall
    /// back to <paramref name="defaultFileName"/> at the same layer
    /// before moving on to the next.
    /// </summary>
    string? Locate(string? subFolder, string fileName, string defaultFileName);
}
