# LlamaShears.Core.Abstractions.Agent.AgentConfigExtensions

Assembly: `LlamaShears.Core.Abstractions`

Convenience accessors for pulling the active [AgentConfig](AgentConfig.md) off
an [IDataContextScope](../Common/IDataContextScope.md) without callers having to remember the
well-known key.

## Methods

### `GetAgentConfig`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the [AgentConfig](AgentConfig.md) attached to the given scope under
[AgentConfig](AgentConfig.md).`DataKey`. Throws when the scope is
`null` or has no config stashed; intended for sites
that legitimately cannot proceed without one.

#### Parameters

- `scope` — Data-context scope to inspect.

### `TryGetAgentConfig`([IDataContextScope](../Common/IDataContextScope.md) scope)

Returns the [AgentConfig](AgentConfig.md) attached to the given scope under
[AgentConfig](AgentConfig.md).`DataKey`, or `null` if none is set.

#### Parameters

- `scope` — Data-context scope to inspect.

#### Returns

The active agent configuration, or `null` when the scope has none.

