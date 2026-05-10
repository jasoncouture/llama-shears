namespace LlamaShears.Core.Abstractions.PromptContext;

/// <summary>
/// Resolves the per-turn prompt-context block that is rendered alongside the
/// system prompt. Implementations look up a Scriban template by name and
/// render it against the supplied data bag.
/// </summary>
public interface IPromptContextProvider
{
    /// <summary>
    /// Renders the prompt-context template for the current turn.
    /// </summary>
    /// <param name="templateName">Name of the prompt-context template; <see langword="null"/> selects the framework default.</param>
    /// <param name="data">Template parameters made available under their keys inside the Scriban scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered prompt-context block, or <see langword="null"/> when the template resolves to nothing.</returns>
    ValueTask<string?> GetAsync(
        string? templateName,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken);
}
