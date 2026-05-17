# LlamaShears.Core.Abstractions.Agent.AgentStateExtensions

Assembly: `LlamaShears.Core.Abstractions`

Convenience accessors for pulling the active [AgentState](AgentState.md) off
an [IDataContextScope](../Common/IDataContextScope.md) without callers having to remember the
well-known key, plus per-field shortcuts that delegate to the same lookup.

## Methods

### `GetAgentState`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the [AgentState](AgentState.md) attached to the given scope under
[AgentState](AgentState.md).`DataKey`. Throws when the scope is
`null` or has no state stashed.

### `GetChannelId`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the active channel id. Throws when no agent state is set.

### `GetCorrelationId`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the active correlation id. Throws when no agent state is set.

### `GetEventId`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the active event id. Throws when no agent state is set.

### `TryGetAgentState`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the [AgentState](AgentState.md) attached to the given scope under
[AgentState](AgentState.md).`DataKey`, or `null` if none is set.

### `TryGetChannelId`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the active channel id, or `null` when no agent state is set.

### `TryGetCorrelationId`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the active correlation id, or `null` when no agent state is set.

### `TryGetEventId`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the active event id, or `null` when no agent state is set.

