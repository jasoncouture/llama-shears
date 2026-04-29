namespace LlamaShears.Agent.Core;

public sealed record AgentConfig
{
    public required AgentModelConfig Model { get; init; }

    public TimeSpan HeartbeatPeriod { get; init; } = TimeSpan.FromMinutes(1);

    public string? SystemPrompt { get; init; }

    public string? SeedTurn { get; init; }
}
