namespace LlamaShears.Core.Abstractions.Provider;

public interface ILanguageModel
{
    IAsyncEnumerable<IModelResponseFragment> PromptAsync(ModelPrompt prompt, CancellationToken cancellationToken);
}
