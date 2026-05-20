# LlamaShears.Core.Abstractions.Events.Agent.AgentStartRequest

Assembly: `LlamaShears.Core.Abstractions`

Payload for [Command](../Event/WellKnown/Command.md).`AgentStart`. Hands a cold
[AgentHandle](../../../AgentHandle.md) built by `IAgentFactory` off to the host, which is responsible
for registering it in the repository and starting its loop.

## Parameters

- `Handle` — The cold handle to start.

## Properties

### `Handle`

The cold handle to start.

## Methods

### `AgentStartRequest`([AgentHandle](../../../AgentHandle.md) Handle)

Payload for [Command](../Event/WellKnown/Command.md).`AgentStart`. Hands a cold
[AgentHandle](../../../AgentHandle.md) built by `IAgentFactory` off to the host, which is responsible
for registering it in the repository and starting its loop.

#### Parameters

- `Handle` — The cold handle to start.

