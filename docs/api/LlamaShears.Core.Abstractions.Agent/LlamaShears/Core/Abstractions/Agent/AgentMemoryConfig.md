# LlamaShears.Core.Abstractions.Agent.AgentMemoryConfig

Assembly: `LlamaShears.Core.Abstractions.Agent`

Per-agent memory-subsystem options.

## Parameters

- `Prefetch` — When `true`, the agent kicks off the per-batch memory
search the moment an inbound `ChannelMessage` lands at its event
handler — concurrently with whatever the agent is doing right then —
instead of waiting until the batch reaches the search step. The win is
overlap: embedding-model latency hides behind work the agent was doing
anyway. Falls back to a synchronous search if the prefetch slot is
missing on a given batch.

## Properties

### `Prefetch`

When `true`, the agent kicks off the per-batch memory
search the moment an inbound `ChannelMessage` lands at its event
handler — concurrently with whatever the agent is doing right then —
instead of waiting until the batch reaches the search step. The win is
overlap: embedding-model latency hides behind work the agent was doing
anyway. Falls back to a synchronous search if the prefetch slot is
missing on a given batch.

## Methods

### `AgentMemoryConfig`(bool Prefetch)

Per-agent memory-subsystem options.

#### Parameters

- `Prefetch` — When `true`, the agent kicks off the per-batch memory
search the moment an inbound `ChannelMessage` lands at its event
handler — concurrently with whatever the agent is doing right then —
instead of waiting until the batch reaches the search step. The win is
overlap: embedding-model latency hides behind work the agent was doing
anyway. Falls back to a synchronous search if the prefetch slot is
missing on a given batch.

