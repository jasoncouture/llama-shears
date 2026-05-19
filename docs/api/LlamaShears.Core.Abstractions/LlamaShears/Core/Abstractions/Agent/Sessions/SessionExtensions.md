# LlamaShears.Core.Abstractions.Agent.Sessions.SessionExtensions

Assembly: `LlamaShears.Core.Abstractions`

Convenience accessors for pulling the active [SessionId](SessionId.md) off
an [IDataContextScope](../../Common/IDataContextScope.md) without callers having to remember the
well-known key.

## Methods

### `GetSessionId`([IDataContextScope](../../Common/IDataContextScope.md) scope)

Returns the [SessionId](SessionId.md) attached to the given scope under
[SessionId](SessionId.md).`DataKey`. Throws when the scope is
`null` or has no session stashed.

#### Parameters

- `scope` — Data-context scope to inspect.

### `TryGetSessionId`([IDataContextScope](../../Common/IDataContextScope.md) scope)

Returns the [SessionId](SessionId.md) attached to the given scope under
[SessionId](SessionId.md).`DataKey`, or `null` if none is set.

#### Parameters

- `scope` — Data-context scope to inspect.

