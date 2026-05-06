namespace LlamaShears.Provider.Abstractions;

public interface ILanguageModel
{
    IAsyncEnumerable<IModelResponseFragment> PromptAsync(ModelPrompt prompt, CancellationToken cancellationToken);
}
