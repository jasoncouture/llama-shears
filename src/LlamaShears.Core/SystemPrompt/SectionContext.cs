namespace LlamaShears.Core.SystemPrompt;

public sealed record SectionContext
{
    public string? Heartbeat { get; init; }

    public string? SubagentSpawning { get; init; }
}
