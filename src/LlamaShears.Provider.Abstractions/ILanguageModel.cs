namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// Base interface for all language models.
/// </summary>
public interface ILanguageModel
{
    IAsyncEnumerable<IModelResponseFragment> PromptAsync(ModelPrompt prompt, CancellationToken cancellationToken);
}
