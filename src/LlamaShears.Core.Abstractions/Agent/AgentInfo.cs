namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Catalog entry returned by <see cref="IAgentFactory.ListAgentsAsync"/>:
/// enough metadata to surface an agent in a list without loading it.
/// </summary>
/// <param name="AgentId">Stable identifier of the agent.</param>
/// <param name="ModelId">Identifier of the language model the agent is wired to.</param>
/// <param name="ContextWindowSize">Token budget the agent's model exposes for a single turn.</param>
/// <param name="Parameters">Free-form metadata surfaced by the factory; <see langword="null"/> = none.</param>
public record AgentInfo(
    string AgentId,
    string ModelId,
    int ContextWindowSize,
    IReadOnlyDictionary<string, object>? Parameters = null);
