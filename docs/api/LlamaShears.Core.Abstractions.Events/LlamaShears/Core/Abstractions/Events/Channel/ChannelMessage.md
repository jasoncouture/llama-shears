# LlamaShears.Core.Abstractions.Events.Channel.ChannelMessage

Assembly: `LlamaShears.Core.Abstractions.Events`

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

