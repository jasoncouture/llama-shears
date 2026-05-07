namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Construction-time inputs for <see cref="IAgentFactory.CreateAgent"/>.
/// Carries the agent identifier and a free-form parameter bag so plugin
/// factories can receive options without growing the framework contract.
/// </summary>
/// <param name="AgentId">Identifier of the agent to construct.</param>
/// <param name="Parameters">Optional free-form construction parameters; <see langword="null"/> = none.</param>
public record AgentConfiguration(
    string AgentId,
    IReadOnlyDictionary<string, object>? Parameters = null);
