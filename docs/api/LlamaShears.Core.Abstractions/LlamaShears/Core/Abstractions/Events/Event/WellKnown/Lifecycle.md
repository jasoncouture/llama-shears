# LlamaShears.Core.Abstractions.Events.Event.WellKnown.Lifecycle

Assembly: `LlamaShears.Core.Abstractions`

Agent lifecycle events keyed on the agent id.

## Properties

### `Birth`

A new agent config has appeared and a root agent should be spawned. Payload is the [AgentConfig](../../../Agent/AgentConfig.md).

### `Death`

An agent config has been removed and its tree should be torn down. Payload is the [AgentDeath](../../Agent/AgentDeath.md) singleton.

### `Update`

An existing agent's config has been mutated. Payload contract is owned elsewhere.

