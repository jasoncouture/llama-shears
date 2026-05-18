# LlamaShears.Core.Abstractions.Events.Agent.AgentLoadRequest

Assembly: `LlamaShears.Core.Abstractions`

Payload for [Command](../Event/WellKnown/Command.md).`AgentLoad`. Carries
the resolved [AgentConfig](../../Agent/AgentConfig.md) the manager should bring up
(or replace an existing slot with). [EventType](../EventType.md).`Id` on the
envelope holds the target agent id.

## Parameters

- `Config` — Immutable agent configuration snapshot to load.

## Properties

### `Config`

Immutable agent configuration snapshot to load.

## Methods

### `AgentLoadRequest`([AgentConfig](../../Agent/AgentConfig.md) Config)

Payload for [Command](../Event/WellKnown/Command.md).`AgentLoad`. Carries
the resolved [AgentConfig](../../Agent/AgentConfig.md) the manager should bring up
(or replace an existing slot with). [EventType](../EventType.md).`Id` on the
envelope holds the target agent id.

#### Parameters

- `Config` — Immutable agent configuration snapshot to load.

