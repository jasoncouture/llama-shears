namespace LlamaShears.Core.Abstractions.PromptContext;

public interface IPromptContextProvider
{
    ValueTask<string?> GetAsync(
        string? templateName,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken);
}
