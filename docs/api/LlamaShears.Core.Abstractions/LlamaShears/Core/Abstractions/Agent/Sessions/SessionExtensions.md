# LlamaShears.Core.Abstractions.Agent.Sessions.SessionExtensions

Assembly: `LlamaShears.Core.Abstractions`

Convenience accessors for pulling the active [SessionPath](SessionPath.md) off
an [IDataContextScope](../../Common/IDataContextScope.md) without callers having to remember the
well-known key.

## Methods

### `GetCurrentSessionId`([IDataContextScope](../../Common/IDataContextScope.md) scope)

Returns the [SessionPath](SessionPath.md).`Current` session id for the scope.

### `GetParentSessionId`([IDataContextScope](../../Common/IDataContextScope.md) scope)

Returns the [SessionPath](SessionPath.md).`Parent` session id for the scope.

### `GetRootSessionId`([IDataContextScope](../../Common/IDataContextScope.md) scope)

Returns the [SessionPath](SessionPath.md).`Root` session id for the scope.

### `GetSessionPath`([IDataContextScope](../../Common/IDataContextScope.md) scope)

Returns the [SessionPath](SessionPath.md) stashed on `scope`. Throws when the
scope is `null` or has no path set.

### `IsRootSession`([IDataContextScope](../../Common/IDataContextScope.md) scope)

`true` when the scope's session path is a root session (no parent above it).

### `TryGetSessionPath`([IDataContextScope](../../Common/IDataContextScope.md) scope)

Returns the [SessionPath](SessionPath.md) stashed on `scope` under
[SessionPath](SessionPath.md).`DataKey`, or `null` when the scope is
`null` or has no path set.

