using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Agent;

public sealed record AgentConfig
{
    [JsonIgnore]
    public string Id { get; init; } = string.Empty;

    public required AgentModelConfig Model { get; init; }
    public string? WorkspacePath { get; init; } = null;

    public TimeSpan HeartbeatPeriod { get; init; } = TimeSpan.FromMinutes(30);

    public string? SystemPrompt { get; init; }
}
