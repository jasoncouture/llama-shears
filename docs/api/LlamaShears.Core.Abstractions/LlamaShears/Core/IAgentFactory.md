# LlamaShears.Core.IAgentFactory

Assembly: `LlamaShears.Core.Abstractions`

Spawns a clean agent state: blank execution context, fresh DI scope, fresh keyed data context seeded with the
supplied [AgentConfig](Abstractions/Agent/AgentConfig.md) plus any caller-supplied overlay data, eager-resolved language model, and a
started [IAgent](Abstractions/Agent/IAgent.md). Returns the [AgentHandle](AgentHandle.md) that owns the resulting scope.

