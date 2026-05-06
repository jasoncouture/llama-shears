namespace LlamaShears.Core.Abstractions.PromptContext;

/// <summary>
/// One memory hit surfaced to the per-turn prompt-context template
/// (<see cref="IPromptContextProvider"/>). The agent reads the body
/// from disk via the read-file tool when it actually wants the
/// content; the template only sees the summary and score.
/// </summary>
/// <param name="RelativePath">Workspace-relative path to the memory file.</param>
/// <param name="Summary">Short summary line surfaced to the model.</param>
/// <param name="Score">Cosine-similarity score against the search query, in [0, 1].</param>
public sealed record PromptContextMemory(string RelativePath, string Summary, double Score);
