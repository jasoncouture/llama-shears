# LlamaShears.Core.Abstractions.Agent.AgentInfo

Assembly: `LlamaShears.Core.Abstractions`

Lightweight catalog entry describing a known agent session: enough
metadata to render an agent in a list or pick one for routing
without loading the full [AgentConfig](AgentConfig.md).

## Parameters

- `Session` — Session this entry identifies.
- `ModelId` — Globally unique identifier of the language model the agent is wired to.
- `ContextWindowSize` — Token budget the agent's model exposes for a single turn.
- `Parameters` — Free-form metadata surfaced by the producer; `null` = none.

## Properties

### `AgentId`

Convenience accessor for [SessionId](Sessions/SessionId.md).`AgentId`.

### `ContextWindowSize`

Token budget the agent's model exposes for a single turn.

### `ModelId`

Globally unique identifier of the language model the agent is wired to.

### `Parameters`

Free-form metadata surfaced by the producer; `null` = none.

### `Session`

Session this entry identifies.

## Methods

### `AgentInfo`([SessionId](Sessions/SessionId.md) Session, [CompositeIdentity](../Common/CompositeIdentity.md) ModelId, int ContextWindowSize, IReadOnlyDictionary<string, object> Parameters)

Lightweight catalog entry describing a known agent session: enough
metadata to render an agent in a list or pick one for routing
without loading the full [AgentConfig](AgentConfig.md).

#### Parameters

- `Session` — Session this entry identifies.
- `ModelId` — Globally unique identifier of the language model the agent is wired to.
- `ContextWindowSize` — Token budget the agent's model exposes for a single turn.
- `Parameters` — Free-form metadata surfaced by the producer; `null` = none.

