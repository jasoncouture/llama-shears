# LlamaShears.Core.Abstractions.Agent.AgentInfo

Assembly: `LlamaShears.Core.Abstractions`

Lightweight catalog entry describing a known agent: enough metadata to
render an agent in a list or pick one for routing without loading the
full [AgentConfig](AgentConfig.md).

## Parameters

- `AgentId` — Stable identifier of the agent.
- `ModelId` — Globally unique identifier of the language model the agent is wired to.
- `ContextWindowSize` — Token budget the agent's model exposes for a single turn.
- `Parameters` — Free-form metadata surfaced by the producer; `null` = none.

## Properties

### `AgentId`

Stable identifier of the agent.

### `ContextWindowSize`

Token budget the agent's model exposes for a single turn.

### `ModelId`

Globally unique identifier of the language model the agent is wired to.

### `Parameters`

Free-form metadata surfaced by the producer; `null` = none.

## Methods

### `AgentInfo`(string AgentId, [ModelIdentity](../Provider/ModelIdentity.md) ModelId, int ContextWindowSize, IReadOnlyDictionary<string, object> Parameters)

Lightweight catalog entry describing a known agent: enough metadata to
render an agent in a list or pick one for routing without loading the
full [AgentConfig](AgentConfig.md).

#### Parameters

- `AgentId` — Stable identifier of the agent.
- `ModelId` — Globally unique identifier of the language model the agent is wired to.
- `ContextWindowSize` — Token budget the agent's model exposes for a single turn.
- `Parameters` — Free-form metadata surfaced by the producer; `null` = none.

