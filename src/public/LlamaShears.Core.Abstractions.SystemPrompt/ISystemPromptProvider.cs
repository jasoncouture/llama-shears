namespace LlamaShears.Core.Abstractions.SystemPrompt;

public interface ISystemPromptProvider
{
    ValueTask<string> GetAsync(
        string? templateName,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken);
}
