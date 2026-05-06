namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Convenience extensions over <see cref="ILanguageModel"/>.
/// </summary>
public static class LanguageModelExtensions
{
    /// <summary>
    /// Calls <see cref="ILanguageModel.PromptAsync"/> with no per-call
    /// option overrides — equivalent to passing <see langword="null"/>
    /// for the options argument.
    /// </summary>
    public static IAsyncEnumerable<IModelResponseFragment> PromptAsync(
        this ILanguageModel model,
        ModelPrompt prompt,
        CancellationToken cancellationToken) => model.PromptAsync(prompt, null, cancellationToken);
}
