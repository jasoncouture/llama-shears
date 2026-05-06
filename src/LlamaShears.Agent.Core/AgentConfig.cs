namespace LlamaShears.Agent.Core;

public sealed record AgentConfig
{
    public required string ProviderName { get; init; }

    public required string ModelId { get; init; }

    public TimeSpan HeartbeatPeriod { get; init; } = TimeSpan.FromMinutes(1);

    public string? SystemPrompt { get; init; }

    public string? SeedTurn { get; init; }
}
