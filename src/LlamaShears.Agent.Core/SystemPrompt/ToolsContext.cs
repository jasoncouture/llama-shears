namespace LlamaShears.Agent.Core.SystemPrompt;

public sealed record ToolsContext
{
    public required string List { get; init; }
}
