using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent.Events;

public sealed record AgentTurnEmitted(string AgentId, ModelTurn Turn);
