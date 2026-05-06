# LlamaShears.Core.Abstractions.Agent.AgentInfo

Assembly: `LlamaShears.Core.Abstractions.Agent`

Catalog entry returned by [IAgentFactory](IAgentFactory.md).`ListAgentsAsync`:
enough metadata to surface an agent in a list without loading it.

## Parameters

- `AgentId` — Stable identifier of the agent.
- `ModelId` — Identifier of the language model the agent is wired to.
- `ContextWindowSize` — Token budget the agent's model exposes for a single turn.
- `Parameters` — Free-form metadata surfaced by the factory; `null` = none.

## Properties

### `AgentId`

Stable identifier of the agent.

### `ContextWindowSize`

Token budget the agent's model exposes for a single turn.

### `ModelId`

Identifier of the language model the agent is wired to.

### `Parameters`

Free-form metadata surfaced by the factory; `null` = none.

## Methods

### `AgentInfo`(string AgentId, string ModelId, int ContextWindowSize, IReadOnlyDictionary<string, object> Parameters)

Catalog entry returned by [IAgentFactory](IAgentFactory.md).`ListAgentsAsync`:
enough metadata to surface an agent in a list without loading it.

#### Parameters

- `AgentId` — Stable identifier of the agent.
- `ModelId` — Identifier of the language model the agent is wired to.
- `ContextWindowSize` — Token budget the agent's model exposes for a single turn.
- `Parameters` — Free-form metadata surfaced by the factory; `null` = none.

