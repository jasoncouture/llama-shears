namespace LlamaShears.Core.Abstractions.SystemPrompt;

public sealed record SystemPromptTemplateParameters(
    string? AgentId = null,
    string? WorkspacePath = null);
