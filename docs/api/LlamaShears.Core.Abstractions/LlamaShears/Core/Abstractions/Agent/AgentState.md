# LlamaShears.Core.Abstractions.Agent.AgentState

Assembly: `LlamaShears.Core.Abstractions`

Per-turn agent state surfaced in the data context. Top-level keys
in the data context are objects, not primitives, so anything an
agent wants to expose to templates or downstream consumers rides
under this single record.

## Parameters

- `ChannelId` — The channel the work in progress is running on (e.g. the channel a user message arrived on, or a synthetic name for non-channel work like `compactor`).
- `EventId` — The well-known event id stamped on outgoing fragments for this turn.
- `CorrelationId` — Correlation id shared by every fragment/event emitted during this turn.

## Fields

### `DataKey`

Key used to stash the active [AgentState](AgentState.md) in the data-context scope.

## Properties

### `ChannelId`

The channel the work in progress is running on (e.g. the channel a user message arrived on, or a synthetic name for non-channel work like `compactor`).

### `CorrelationId`

Correlation id shared by every fragment/event emitted during this turn.

### `EventId`

The well-known event id stamped on outgoing fragments for this turn.

## Methods

### `AgentState`(string ChannelId, string EventId, Guid CorrelationId)

Per-turn agent state surfaced in the data context. Top-level keys
in the data context are objects, not primitives, so anything an
agent wants to expose to templates or downstream consumers rides
under this single record.

#### Parameters

- `ChannelId` — The channel the work in progress is running on (e.g. the channel a user message arrived on, or a synthetic name for non-channel work like `compactor`).
- `EventId` — The well-known event id stamped on outgoing fragments for this turn.
- `CorrelationId` — Correlation id shared by every fragment/event emitted during this turn.

