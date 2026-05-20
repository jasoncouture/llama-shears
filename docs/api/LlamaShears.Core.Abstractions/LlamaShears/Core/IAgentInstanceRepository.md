# LlamaShears.Core.IAgentInstanceRepository

Assembly: `LlamaShears.Core.Abstractions`

Tracks every [AgentHandle](AgentHandle.md) across the host, keyed by session id, with knowledge
of the parent/child graph. Enumerations expose handles in safe disposal order.

## Methods

### `AddAgent`([AgentHandle](AgentHandle.md) handle)

Adds an agent to the repository.

#### Parameters

- `handle` — The [AgentHandle](AgentHandle.md) to add. The dictionary key is taken from [SessionPath](Abstractions/Agent/Sessions/SessionPath.md)'s id property.

#### Exceptions

- InvalidOperationException — Thrown when the handle's session id is already present, or when its parent/root is unknown.

### `DescendentsOf`(Guid parentId)

Returns all descendants of `parentId`, with outermost leaves first —
a safe stop/dispose order.

#### Parameters

- `parentId` — The parent whose children to enumerate.

#### Returns

Children in disposal/stop order.

### `GetAgent`(Guid id)

Gets an [AgentHandle](AgentHandle.md) by its id.

#### Parameters

- `id` — The id of the agent to get.

#### Returns

The agent.

#### Exceptions

- KeyNotFoundException — Thrown when the id is not found.

### `GetAgentInstancesByName`(string name)

Returns the ids of every tracked instance whose session name matches `name`
(case-insensitive).

### `GetAllAgents`

Gets all handles.

#### Returns

Enumerable of agent handles; ordering is safe for disposal.

### `Remove`(Guid id, [AgentHandle](AgentHandle.md)& handle)

Removes the handle with id `id`. Throws if the handle still has children.

#### Parameters

- `id` — Id of the handle to remove.
- `handle` — The removed handle, not null when the function returns `true`.

#### Returns

`true` when removed.

#### Exceptions

- InvalidOperationException — Thrown when descendants exist and must be removed first.

### `RemoveDescendents`(Guid parentId, bool includeParent)

Removes all descendants of `parentId`, optionally including the parent itself.

#### Parameters

- `parentId` — Parent whose subtree to remove.
- `includeParent` — `true` to remove the parent as well, `false` for children only.

### `TryGetAgent`(Guid id, [AgentHandle](AgentHandle.md)& handle)

Gets an [AgentHandle](AgentHandle.md) by its id.

#### Parameters

- `id` — The id to find.
- `handle` — The found agent handle, not null if the function returns `true`.

#### Returns

`true` if the handle was found, `false` otherwise.

### `TryGetDefaultSession`(string agentId, Guid& id)

Returns the session id of the default (root) session for
`agentId`, when one exists.

#### Parameters

- `agentId` — Agent whose default session is being looked up.
- `id` — Session id of the default session on success.

#### Returns

`true` when a default session is registered; `false` otherwise.

### `TryRemove`(Guid id, [AgentHandle](AgentHandle.md)& handle)

Removes the handle only when it has no descendants. Returns `false` when descendants remain.

