# LlamaShears.Core.Abstractions.Agent.Sessions.SessionPath

Assembly: `LlamaShears.Core.Abstractions`

Parent/root chain for an agent session. [SessionPath](SessionPath.md).`Current` identifies this session;
[SessionPath](SessionPath.md).`Parent` and [SessionPath](SessionPath.md).`Root` identify the ancestor in the session tree. For
a root session all three refer to the same [SessionId](SessionId.md).

## Parameters

- `Current` — Session id of this session.
- `Parent` — Session id of this session's parent; equals `Current` for a root.
- `Root` — Session id of the tree's root; equals `Current` for a root.

## Fields

### `DataKey`

Key used to stash the active [SessionPath](SessionPath.md) in the per-turn data context scope.

## Properties

### `Current`

Session id of this session.

### `Id`

Guid of [SessionPath](SessionPath.md).`Current`.

### `IsRootSession`

`true` when this path represents a root session (no parent above it).

### `Parent`

Session id of this session's parent; equals `Current` for a root.

### `ParentId`

Guid of [SessionPath](SessionPath.md).`Parent`.

### `Root`

Session id of the tree's root; equals `Current` for a root.

### `RootId`

Guid of [SessionPath](SessionPath.md).`Root`.

## Methods

### `SessionPath`([SessionId](SessionId.md) current)

Builds a root session path where current, parent, and root all reference `current`.

### `SessionPath`([SessionId](SessionId.md) Current, [SessionId](SessionId.md) Parent, [SessionId](SessionId.md) Root)

Parent/root chain for an agent session. [SessionPath](SessionPath.md).`Current` identifies this session;
[SessionPath](SessionPath.md).`Parent` and [SessionPath](SessionPath.md).`Root` identify the ancestor in the session tree. For
a root session all three refer to the same [SessionId](SessionId.md).

#### Parameters

- `Current` — Session id of this session.
- `Parent` — Session id of this session's parent; equals `Current` for a root.
- `Root` — Session id of the tree's root; equals `Current` for a root.

### `CreateChildSession`([SessionId](SessionId.md) session)

Creates a child session path using this path as the parent.

#### Parameters

- `session` — Child session id.

#### Returns

Session path with the current instance as the parent and the same root.

### `GetData`

### `ToString`

