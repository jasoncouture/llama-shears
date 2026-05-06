# LlamaShears.Core.Abstractions.Events.Agent.AgentThoughtFragment

Assembly: `LlamaShears.Core.Abstractions.Events`

One streaming chunk of hidden chain-of-thought emitted by a
thinking-capable model. Surfaced for visibility but never replayed
back into a later prompt.

## Parameters

- `Content` — Reasoning text in this fragment.
- `ChannelId` — Optional channel correlation id; `null` when not channel-bound.
- `Final` — Whether this is the last fragment for the current thought stream.

## Properties

### `Final`

Whether this is the last fragment for the current thought stream.

## Methods

### `AgentThoughtFragment`(string Content, string ChannelId, bool Final)

One streaming chunk of hidden chain-of-thought emitted by a
thinking-capable model. Surfaced for visibility but never replayed
back into a later prompt.

#### Parameters

- `Content` — Reasoning text in this fragment.
- `ChannelId` — Optional channel correlation id; `null` when not channel-bound.
- `Final` — Whether this is the last fragment for the current thought stream.

