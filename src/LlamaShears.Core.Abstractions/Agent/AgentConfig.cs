using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Agent;

public sealed record AgentConfig(
    [property: JsonRequired] AgentModelConfig Model,
    [property: JsonPropertyName("mcpServers")]ImmutableDictionary<string, Uri> ModelContextProtocolServers,
    [property: JsonIgnore] string Id = "",
    string? WorkspacePath = null,
    string? SystemPrompt = null)
{
    public TimeSpan HeartbeatPeriod { get; init; } = TimeSpan.FromMinutes(30);
}
