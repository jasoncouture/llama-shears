# LlamaShears.Core.Abstractions.Agent.Todo.TodoItemUpdate

Assembly: `LlamaShears.Core.Abstractions`

Patch applied to a single [TodoItem](TodoItem.md) when the agent toggles
its completion state.

## Parameters

- `Index` — 1-based ordinal of the item to update; matches [TodoItem](TodoItem.md).`Index`.
- `IsCompleted` — Target completion state for the addressed item.

## Properties

### `Index`

1-based ordinal of the item to update; matches [TodoItem](TodoItem.md).`Index`.

### `IsCompleted`

Target completion state for the addressed item.

## Methods

### `TodoItemUpdate`(int Index, bool IsCompleted)

Patch applied to a single [TodoItem](TodoItem.md) when the agent toggles
its completion state.

#### Parameters

- `Index` — 1-based ordinal of the item to update; matches [TodoItem](TodoItem.md).`Index`.
- `IsCompleted` — Target completion state for the addressed item.

