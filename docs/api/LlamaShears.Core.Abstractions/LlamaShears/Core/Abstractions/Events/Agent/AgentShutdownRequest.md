# LlamaShears.Core.Abstractions.Events.Agent.AgentShutdownRequest

Assembly: `LlamaShears.Core.Abstractions`

Payload for [Command](../Event/WellKnown/Command.md).`AgentShutdown`. The target session
shuts itself down — cancels its loop, awaits drain, publishes its own
`agent:stopped` lifecycle event.

## Parameters

- `SessionId` — The specific agent boot to shut down. Subscribers match this against their own [AgentShutdownRequest](AgentShutdownRequest.md).`SessionId` and ignore otherwise. If [AgentShutdownRequest](AgentShutdownRequest.md).`SessionId` is `null`, it's a broadcast stop command.

## Properties

### `SessionId`

The specific agent boot to shut down. Subscribers match this against their own [AgentShutdownRequest](AgentShutdownRequest.md).`SessionId` and ignore otherwise. If [AgentShutdownRequest](AgentShutdownRequest.md).`SessionId` is `null`, it's a broadcast stop command.

## Methods

### `AgentShutdownRequest`([SessionId](../../Agent/Sessions/SessionId.md) SessionId)

Payload for [Command](../Event/WellKnown/Command.md).`AgentShutdown`. The target session
shuts itself down — cancels its loop, awaits drain, publishes its own
`agent:stopped` lifecycle event.

#### Parameters

- `SessionId` — The specific agent boot to shut down. Subscribers match this against their own [AgentShutdownRequest](AgentShutdownRequest.md).`SessionId` and ignore otherwise. If [AgentShutdownRequest](AgentShutdownRequest.md).`SessionId` is `null`, it's a broadcast stop command.

