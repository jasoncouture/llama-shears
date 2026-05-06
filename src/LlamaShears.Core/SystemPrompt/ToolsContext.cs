namespace LlamaShears.Core.SystemPrompt;

public sealed record ToolsContext
{
    public required string List { get; init; }
}
