namespace LlamaShears.Core;

public sealed record AgentConfig
{
    public required AgentModelConfig Model { get; init; }

    public TimeSpan HeartbeatPeriod { get; init; } = TimeSpan.FromMinutes(30);
}
