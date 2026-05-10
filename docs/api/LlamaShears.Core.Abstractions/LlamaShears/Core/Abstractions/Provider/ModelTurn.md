# LlamaShears.Core.Abstractions.Provider.ModelTurn

Assembly: `LlamaShears.Core.Abstractions`

One entry in an agent's conversation log. Carries the speaker role,
body text, and any tool-call or attachment metadata associated with
the turn.

## Parameters

- `Role` — Who/what authored this turn.
- `Content` — Body text of the turn.
- `Timestamp` — When the turn was recorded.
- `ChannelId` — Channel correlation id for routing turns back into a multi-channel UI; `null` when the turn has no channel context.
- `Ephemeral` — When `true`, the turn is transient: subscribers may
observe it (e.g. UI streaming) but persisters skip it. Drives the
"don't record this" decision from the turn itself instead of from a
central filter.

## Properties

### `Attachments`

Non-text payloads attached to this turn (images, etc.).

### `ChannelId`

Channel correlation id for routing turns back into a multi-channel UI; `null` when the turn has no channel context.

### `Content`

Body text of the turn.

### `Ephemeral`

When `true`, the turn is transient: subscribers may
observe it (e.g. UI streaming) but persisters skip it. Drives the
"don't record this" decision from the turn itself instead of from a
central filter.

### `IsError`

True when this turn represents a failed tool call or framework-level error.

### `Role`

Who/what authored this turn.

### `Timestamp`

When the turn was recorded.

### `ToolCall`

The tool call this turn is the result of (tool turns only); `null` otherwise.

### `ToolCalls`

Tool calls the model emitted on this turn (assistant turns only).

## Methods

### `ModelTurn`([ModelRole](ModelRole.md) Role, string Content, DateTimeOffset Timestamp, string ChannelId, bool Ephemeral)

One entry in an agent's conversation log. Carries the speaker role,
body text, and any tool-call or attachment metadata associated with
the turn.

#### Parameters

- `Role` — Who/what authored this turn.
- `Content` — Body text of the turn.
- `Timestamp` — When the turn was recorded.
- `ChannelId` — Channel correlation id for routing turns back into a multi-channel UI; `null` when the turn has no channel context.
- `Ephemeral` — When `true`, the turn is transient: subscribers may
observe it (e.g. UI streaming) but persisters skip it. Drives the
"don't record this" decision from the turn itself instead of from a
central filter.

