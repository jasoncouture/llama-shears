# LlamaShears.Core.IAgentFactory

Assembly: `LlamaShears.Core.Abstractions`

Spawns a clean agent state: blank execution context, fresh DI scope, fresh keyed data context seeded with the
supplied [AgentConfig](Abstractions/Agent/AgentConfig.md) plus any caller-supplied overlay data, eager-resolved language model, and a
started [IAgent](Abstractions/Agent/IAgent.md). Returns the [AgentHandle](AgentHandle.md) that owns the resulting scope.

## Methods

### `CreateAgentAsync`([AgentConfig](Abstractions/Agent/AgentConfig.md) config, [SessionPath](Abstractions/Agent/Sessions/SessionPath.md) sessionPath, IEnumerable<KeyValuePair<string, object>> data, CancellationToken cancellationToken)

Creates a new agent handle with the specified parameters.

#### Parameters

- `config` — Agent configuration.
- `sessionPath` — Agent's unique session path.
- `data` — Additional data to inject into the agent's context data scope.
- `cancellationToken` — Cancellation token for the build pipeline.

#### Returns

A ready-to-start, validated [AgentHandle](AgentHandle.md) with a unique execution context.

