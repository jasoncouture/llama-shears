namespace LlamaShears.Core.SystemPrompt;

public sealed record SystemPromptModel
{
    public required WorkspaceContext Workspace { get; init; }

    public required ToolsContext Tools { get; init; }

    public required RuntimeContext Runtime { get; init; }

    public SectionContext? Section { get; init; }

    public SubagentContext? Subagent { get; init; }
}
