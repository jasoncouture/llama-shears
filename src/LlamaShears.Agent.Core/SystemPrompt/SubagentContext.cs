namespace LlamaShears.Agent.Core.SystemPrompt;

public sealed record SubagentContext
{
    public required string Task { get; init; }

    public required string SessionContext { get; init; }
}
