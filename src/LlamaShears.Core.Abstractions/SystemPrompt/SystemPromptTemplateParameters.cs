namespace LlamaShears.Core.Abstractions.SystemPrompt;

/// <summary>
/// Inputs the system-prompt template has access to when rendered by
/// <see cref="ISystemPromptProvider"/>. Templates are Scriban; new
/// values are added here rather than composed in C# so the template
/// stays the single point of authorship.
/// </summary>
/// <param name="AgentId">Identifier of the agent the prompt is being rendered for; <see langword="null"/> when not bound to a specific agent.</param>
/// <param name="WorkspacePath">Absolute workspace path; <see langword="null"/> when not bound to a workspace.</param>
/// <param name="ToolCallTurns">Configured tool-call turn budget for the agent.</param>
public sealed record SystemPromptTemplateParameters(
    string? AgentId = null,
    string? WorkspacePath = null,
    int ToolCallTurns = 0)
{
    /// <summary>Workspace files surfaced to the template (e.g. <c>AGENTS.md</c>, agent-specific manifests).</summary>
    public IReadOnlyList<WorkspaceFile> Files { get; init; } = [];
}
