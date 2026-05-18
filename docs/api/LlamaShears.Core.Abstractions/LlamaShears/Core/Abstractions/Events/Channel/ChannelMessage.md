# LlamaShears.Core.Abstractions.Events.Channel.ChannelMessage

Assembly: `LlamaShears.Core.Abstractions`

One message inbound on a chat channel. Routed onto the event bus so
the agent loop, UI, and any audit subscribers see the same payload.

## Parameters

- `Text` — User-supplied text.
- `AgentId` — Target agent id when the message is addressed to a specific agent; `null` for broadcast/system messages.
- `Timestamp` — When the message was received.

## Properties

### `AgentId`

Target agent id when the message is addressed to a specific agent; `null` for broadcast/system messages.

### `Attachments`

Non-text payloads (e.g. images) attached to this message.

### `SessionId`

Sender's session id when the message originates from a non-default
session (e.g. an ephemeral child session replying to its parent agent).
`null` for default-session senders — user chat, slash
commands, host-side injections. Receivers ignore this field today;
it exists as audit/UI metadata and as the seam for future
session-aware routing.

### `Text`

User-supplied text.

### `Timestamp`

When the message was received.

## Methods

### `ChannelMessage`(string Text, string AgentId, DateTimeOffset Timestamp)

One message inbound on a chat channel. Routed onto the event bus so
the agent loop, UI, and any audit subscribers see the same payload.

#### Parameters

- `Text` — User-supplied text.
- `AgentId` — Target agent id when the message is addressed to a specific agent; `null` for broadcast/system messages.
- `Timestamp` — When the message was received.

