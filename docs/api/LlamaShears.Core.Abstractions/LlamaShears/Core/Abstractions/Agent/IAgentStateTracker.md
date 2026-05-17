# LlamaShears.Core.Abstractions.Agent.IAgentStateTracker

Assembly: `LlamaShears.Core.Abstractions`

Writes the active [AgentState](AgentState.md) into the current data
context scope. Centralizes the construction so every caller stamps
the same shape (channel, event id, correlation id).

## Methods

### `SetState`(string channelId, string eventId, Nullable<Guid> correlationId)

Stashes a fresh [AgentState](AgentState.md) on the current data
context scope under [AgentState](AgentState.md).`DataKey`. When
`eventId` is `null`, the active
[AgentConfig](AgentConfig.md).`Id` is used so the common agent-turn
path doesn't need to repeat it. When `correlationId`
is `null`, a new `Guid.CreateVersion7` is
minted.

