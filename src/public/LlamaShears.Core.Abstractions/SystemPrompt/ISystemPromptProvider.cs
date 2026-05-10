namespace LlamaShears.Core.Abstractions.SystemPrompt;

/// <summary>
/// Resolves the system-prompt block injected at the start of an agent
/// interaction. Implementations look up a Scriban template by file name
/// and render it against the supplied data bag.
/// </summary>
public interface ISystemPromptProvider
{
    /// <summary>
    /// Renders the system prompt for the current turn.
    /// </summary>
    /// <param name="templateName">File name (with extension) of the system-prompt template; <see langword="null"/> selects the framework default.</param>
    /// <param name="data">Template parameters made available under their keys inside the Scriban scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered system-prompt text.</returns>
    ValueTask<string> GetAsync(
        string? templateName,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken);
}
