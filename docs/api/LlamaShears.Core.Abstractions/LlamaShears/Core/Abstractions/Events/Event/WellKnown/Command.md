# LlamaShears.Core.Abstractions.Events.Event.WellKnown.Command

Assembly: `LlamaShears.Core.Abstractions`

Command events targeting a specific agent.

## Properties

### `CompactionRequest`

Caller is requesting a compaction pass against the agent; payload's `Force` drives whether the under-budget guard is bypassed.

### `InterruptAgent`

Caller is requesting that the agent's in-flight turn be interrupted.

