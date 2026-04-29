namespace LlamaShears.Agent.Abstractions;

/// <summary>
/// Metadata describing an agent surfaced by an <see cref="IAgentFactory"/>.
/// </summary>
/// <param name="AgentId">Unique identifier of the agent.</param>
/// <param name="ModelId">Identifier of the model the agent runs on.</param>
/// <param name="ContextWindowSize">Maximum context window the agent uses.</param>
/// <param name="Parameters">Additional agent-specific parameters.</param>
public record AgentInfo(
    string AgentId,
    string ModelId,
    int ContextWindowSize,
    IReadOnlyDictionary<string, object>? Parameters = null);
