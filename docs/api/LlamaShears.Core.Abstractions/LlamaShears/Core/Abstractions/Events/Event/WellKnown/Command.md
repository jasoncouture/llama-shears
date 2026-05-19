# LlamaShears.Core.Abstractions.Events.Event.WellKnown.Command

Assembly: `LlamaShears.Core.Abstractions`

Command events targeting a specific agent.

## Properties

### `AgentLoad`

Config supervisor is asking the agent manager to load or replace an agent. Payload carries the resolved [AgentConfig](../../../Agent/AgentConfig.md).

### `AgentStop`

Caller is asking a specific agent boot to shut itself down. Payload carries the target `SessionId`.

### `AgentUnload`

Config supervisor is asking the agent manager to unload an agent. Payload is the empty marker.

### `CompactionRequest`

Caller is requesting a compaction pass against the agent; payload's `Force` drives whether the under-budget guard is bypassed.

### `InterruptAgent`

Caller is requesting that the agent's in-flight turn be interrupted.

