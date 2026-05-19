# LlamaShears.Core.Abstractions.Events.Event.WellKnown.Command

Assembly: `LlamaShears.Core.Abstractions`

Command events targeting a specific agent.

## Properties

### `AgentLoad`

Config supervisor is asking the agent manager to load or replace an agent. Payload carries the resolved [AgentConfig](../../../Agent/AgentConfig.md).

### `AgentShutdown`

Caller is asking a specific agent boot to shut itself down. Payload carries the target `SessionId`.

### `AgentStart`

Caller is asking the host to register and start a freshly built [AgentHandle](../../../../AgentHandle.md). Payload carries the cold handle.

### `AgentStop`

Caller is asking the host to stop a specific session. Payload carries the non-null target `SessionId`.

### `AgentUnload`

Config supervisor is asking the agent manager to unload an agent. Payload is the empty marker.

### `CompactionRequest`

Caller is requesting a compaction pass against the agent; payload's `Force` drives whether the under-budget guard is bypassed.

### `InterruptAgent`

Caller is requesting that the agent's in-flight turn be interrupted.

