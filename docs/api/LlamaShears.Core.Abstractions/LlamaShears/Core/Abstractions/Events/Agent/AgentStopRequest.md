# LlamaShears.Core.Abstractions.Events.Agent.AgentStopRequest

Assembly: `LlamaShears.Core.Abstractions`

Payload for [Command](../Event/WellKnown/Command.md).`AgentStop`. Targets a specific session that
the host is about to tear down; carries a non-null [AgentStopRequest](AgentStopRequest.md).`SessionId`.

## Parameters

- `SessionId` — Session id whose teardown is being requested.

## Properties

### `SessionId`

Session id whose teardown is being requested.

## Methods

### `AgentStopRequest`([SessionId](../../Agent/Sessions/SessionId.md) SessionId)

Payload for [Command](../Event/WellKnown/Command.md).`AgentStop`. Targets a specific session that
the host is about to tear down; carries a non-null [AgentStopRequest](AgentStopRequest.md).`SessionId`.

#### Parameters

- `SessionId` — Session id whose teardown is being requested.

