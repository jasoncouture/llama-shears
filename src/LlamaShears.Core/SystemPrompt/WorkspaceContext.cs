namespace LlamaShears.Core.SystemPrompt;

public sealed record WorkspaceContext
{
    public required string Directory { get; init; }

    public string? Notes { get; init; }
}
