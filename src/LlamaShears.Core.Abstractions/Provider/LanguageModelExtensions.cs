namespace LlamaShears.Core.Abstractions.Provider;

public static class LanguageModelExtensions
{
    public static IAsyncEnumerable<IModelResponseFragment> PromptAsync(
        this ILanguageModel model,
        ModelPrompt prompt,
        CancellationToken cancellationToken) => model.PromptAsync(prompt, null, cancellationToken);
}
