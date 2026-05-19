# LlamaShears.Core.Abstractions.Events.Agent.AgentStopRequest

Assembly: `LlamaShears.Core.Abstractions`

Payload for [Command](../Event/WellKnown/Command.md).`AgentStop`. The target session
shuts itself down — cancels its loop, awaits drain, publishes its own
`agent:stopped` lifecycle event.

## Parameters

- `SessionId` — The specific agent boot to shut down. Subscribers match this against their own [AgentStopRequest](AgentStopRequest.md).`SessionId` and ignore otherwise. If SessionId is null, it's a broadcast stop command

## Properties

### `SessionId`

The specific agent boot to shut down. Subscribers match this against their own [AgentStopRequest](AgentStopRequest.md).`SessionId` and ignore otherwise. If SessionId is null, it's a broadcast stop command

## Methods

### `AgentStopRequest`([SessionId](../../Agent/Sessions/SessionId.md) SessionId)

Payload for [Command](../Event/WellKnown/Command.md).`AgentStop`. The target session
shuts itself down — cancels its loop, awaits drain, publishes its own
`agent:stopped` lifecycle event.

#### Parameters

- `SessionId` — The specific agent boot to shut down. Subscribers match this against their own [AgentStopRequest](AgentStopRequest.md).`SessionId` and ignore otherwise. If SessionId is null, it's a broadcast stop command

