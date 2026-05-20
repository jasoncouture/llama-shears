# LlamaShears.Core.Abstractions.Events.Channel.ChannelMessage

Assembly: `LlamaShears.Core.Abstractions`

Inbound channel message routed to a specific session. Published on
[Channel](../Event/WellKnown/Channel.md).`Message` with the channel id in
the `Id` segment of the event type.

## Parameters

- `Text` — User-supplied body of the message.
- `ChannelId` — Channel the message originated on (e.g. `webui`, `telegram:123`).
- `Timestamp` — When the message was produced.

## Properties

### `Attachments`

Non-text payloads (e.g. images) attached to this message.

### `ChannelId`

Channel the message originated on (e.g. `webui`, `telegram:123`).

### `Text`

User-supplied body of the message.

### `Timestamp`

When the message was produced.

## Methods

### `ChannelMessage`(string Text, string ChannelId, DateTimeOffset Timestamp)

Inbound channel message routed to a specific session. Published on
[Channel](../Event/WellKnown/Channel.md).`Message` with the channel id in
the `Id` segment of the event type.

#### Parameters

- `Text` — User-supplied body of the message.
- `ChannelId` — Channel the message originated on (e.g. `webui`, `telegram:123`).
- `Timestamp` — When the message was produced.

