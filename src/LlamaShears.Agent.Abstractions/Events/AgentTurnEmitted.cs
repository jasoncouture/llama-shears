using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Abstractions.Events;

public sealed record AgentTurnEmitted(string AgentId, ModelTurn Turn);
