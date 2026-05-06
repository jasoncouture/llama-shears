# LlamaShears.Core.Abstractions.Events.Agent.AgentMessageBase

Assembly: `LlamaShears.Core.Abstractions.Events`

Common shape for agent-emitted message and thought fragments
flowing through the event bus. Concrete subtypes
([AgentMessageFragment](AgentMessageFragment.md), [AgentThoughtFragment](AgentThoughtFragment.md))
add stream-specific metadata.

## Parameters

- `Content` — Body text of this fragment.
- `ChannelId` — Optional channel correlation id; `null` when not channel-bound.

## Properties

### `ChannelId`

Optional channel correlation id; `null` when not channel-bound.

### `Content`

Body text of this fragment.

## Methods

### `AgentMessageBase`(string Content, string ChannelId)

Common shape for agent-emitted message and thought fragments
flowing through the event bus. Concrete subtypes
([AgentMessageFragment](AgentMessageFragment.md), [AgentThoughtFragment](AgentThoughtFragment.md))
add stream-specific metadata.

#### Parameters

- `Content` — Body text of this fragment.
- `ChannelId` — Optional channel correlation id; `null` when not channel-bound.

