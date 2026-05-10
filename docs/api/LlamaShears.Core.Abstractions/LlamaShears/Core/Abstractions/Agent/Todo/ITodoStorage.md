# LlamaShears.Core.Abstractions.Agent.Todo.ITodoStorage

Assembly: `LlamaShears.Core.Abstractions`

Persists the agent's TODO list as a Markdown file at the workspace
root. All mutations rewrite or append to that file; a corrupt file
is reset to the canonical empty state and the result reflects that
recovery.

## Methods

### `AddAsync`(IReadOnlyList<string> items, bool done, CancellationToken cancellationToken)

Appends a batch of items to the list with fresh sequential indices.
All items in `items` share the same
`done` flag; for a mixed batch, call twice.

#### Parameters

- `items` — Item texts. Each must be non-empty, must not contain newline
characters, and must not exceed the configured maximum length.
The whole batch is rejected if any item is invalid.
- `done` — `true` records every new item as already completed.
- `cancellationToken` — Cancellation token.

### `ClearAsync`(bool includeIncomplete, CancellationToken cancellationToken)

Removes items from the list. By default only completed items are
removed; when `includeIncomplete` is
`true` incomplete items are also removed,
effectively wiping the list.

#### Parameters

- `includeIncomplete` — `false` (default) clears completed items only.
`true` also clears incomplete items.
- `cancellationToken` — Cancellation token.

### `DeleteAsync`(IReadOnlyList<int> indices, CancellationToken cancellationToken)

Removes a batch of items by 1-based index and renumbers the
remainder. Duplicate indices are deduped. The whole batch is
rejected if any index is missing.

#### Parameters

- `indices` — 1-based indices of items to delete.
- `cancellationToken` — Cancellation token.

### `ListAsync`(Nullable<int> offset, Nullable<int> limit, CancellationToken cancellationToken)

Returns the current list, optionally paginated.

#### Parameters

- `offset` — Number of items to skip from the start. `null`
or non-positive starts at the beginning.
- `limit` — Maximum number of items to return. `null` or
negative returns all remaining items.
- `cancellationToken` — Cancellation token.

### `UpdateAsync`(IReadOnlyList<[TodoItemUpdate](TodoItemUpdate.md)> updates, CancellationToken cancellationToken)

Applies a batch of completion-state changes. The whole batch is
rejected if any update names a missing index. Updates that match
the current state are silently no-ops; the rewrite still happens
only when at least one change took effect.

#### Parameters

- `updates` — 1-based index plus the target state for each item.
- `cancellationToken` — Cancellation token.

