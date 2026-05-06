using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Core;

public sealed record AgentConfig
{
    public required ModelIdentity Model { get; init; }

    public TimeSpan HeartbeatPeriod { get; init; } = TimeSpan.FromMinutes(1);

    public string? SystemPrompt { get; init; }

    public string? SeedTurn { get; init; }

    public ThinkLevel Think { get; init; } = ThinkLevel.None;
}
