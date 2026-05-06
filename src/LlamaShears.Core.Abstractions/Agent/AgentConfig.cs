using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Agent;

public sealed record AgentConfig(
    [property: JsonRequired] AgentModelConfig Model,
    [property: JsonIgnore] string Id = "",
    string? WorkspacePath = null,
    string? SystemPrompt = null,
    [property: JsonPropertyName("mcpServers")] ImmutableHashSet<string>? ModelContextProtocolServers = null)
{
    public TimeSpan HeartbeatPeriod { get; init; } = TimeSpan.FromMinutes(30);
    public AgentToolConfig Tools { get; init; } = new AgentToolConfig();
}
