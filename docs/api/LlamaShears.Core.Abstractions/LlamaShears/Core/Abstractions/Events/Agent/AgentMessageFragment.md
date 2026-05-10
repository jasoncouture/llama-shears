# LlamaShears.Core.Abstractions.Events.Agent.AgentMessageFragment

Assembly: `LlamaShears.Core.Abstractions`

One streaming chunk of agent-visible text emitted as the model
produces its response. Subscribers concatenate fragments in
arrival order to reconstruct the final assistant message.

## Parameters

- `Content` — Text of this fragment.
- `ChannelId` — Optional channel correlation id; `null` when not channel-bound.
- `Final` — Whether this is the last fragment for the current message stream.

## Properties

### `Final`

Whether this is the last fragment for the current message stream.

## Methods

### `AgentMessageFragment`(string Content, string ChannelId, bool Final)

One streaming chunk of agent-visible text emitted as the model
produces its response. Subscribers concatenate fragments in
arrival order to reconstruct the final assistant message.

#### Parameters

- `Content` — Text of this fragment.
- `ChannelId` — Optional channel correlation id; `null` when not channel-bound.
- `Final` — Whether this is the last fragment for the current message stream.

