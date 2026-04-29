namespace LlamaShears.Agent.Abstractions;

/// <summary>
/// Configuration for creating an agent instance.
/// </summary>
/// <param name="AgentId">Identifier of the agent to create.</param>
/// <param name="Parameters">Agent-specific parameter overrides.</param>
public record AgentConfiguration(
    string AgentId,
    IReadOnlyDictionary<string, object>? Parameters = null);
