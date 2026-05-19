# LlamaShears.Core.Abstractions.Events.Agent.AgentEventInformation

Assembly: `LlamaShears.Core.Abstractions`

Payload carried by agent lifecycle events (`agent:starting`, `agent:started`, `agent:stopping`,
`agent:stopped`) identifying which agent boot the notification refers to.

## Parameters

- `Config` — Config the agent was started with.
- `SessionId` — Session id of the boot — distinguishes the default (main) session from sub-sessions of the same agent.

## Properties

### `Config`

Config the agent was started with.

### `SessionId`

Session id of the boot — distinguishes the default (main) session from sub-sessions of the same agent.

## Methods

### `AgentEventInformation`([AgentConfig](../../Agent/AgentConfig.md) Config, [SessionId](../../Agent/Sessions/SessionId.md) SessionId)

Payload carried by agent lifecycle events (`agent:starting`, `agent:started`, `agent:stopping`,
`agent:stopped`) identifying which agent boot the notification refers to.

#### Parameters

- `Config` — Config the agent was started with.
- `SessionId` — Session id of the boot — distinguishes the default (main) session from sub-sessions of the same agent.

