using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Abstractions.Events;

/// <summary>
/// A complete turn produced by an agent and ready for consumers to
/// render or persist. Fired after the turn has been appended to the
/// agent's <see cref="IAgent.Context"/>.
/// </summary>
/// <param name="AgentId">Identifier of the agent that emitted the turn.</param>
/// <param name="Turn">The turn itself, including role, content, and timestamp.</param>
public sealed record AgentTurnEmitted(string AgentId, ModelTurn Turn);
