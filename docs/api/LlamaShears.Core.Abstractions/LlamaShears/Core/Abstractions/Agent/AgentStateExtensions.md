# LlamaShears.Core.Abstractions.Agent.AgentStateExtensions

Assembly: `LlamaShears.Core.Abstractions`

Convenience accessors for pulling the active [AgentState](AgentState.md) off
an [IDataContextScope](../Common/IDataContextScope.md) without callers having to remember the
well-known key, plus per-field shortcuts that delegate to the same lookup.

## Methods

### `GetAgentState`([IDataContextScope](../Common/IDataContextScope.md) scope)

### `GetChannelId`([IDataContextScope](../Common/IDataContextScope.md) scope)

### `GetCorrelationId`([IDataContextScope](../Common/IDataContextScope.md) scope)

### `GetEventId`([IDataContextScope](../Common/IDataContextScope.md) scope)

### `TryGetAgentState`([IDataContextScope](../Common/IDataContextScope.md) scope)

### `TryGetChannelId`([IDataContextScope](../Common/IDataContextScope.md) scope)

### `TryGetCorrelationId`([IDataContextScope](../Common/IDataContextScope.md) scope)

### `TryGetEventId`([IDataContextScope](../Common/IDataContextScope.md) scope)

