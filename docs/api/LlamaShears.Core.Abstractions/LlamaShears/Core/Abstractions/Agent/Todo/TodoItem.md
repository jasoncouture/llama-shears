# LlamaShears.Core.Abstractions.Agent.Todo.TodoItem

Assembly: `LlamaShears.Core.Abstractions`

Single entry in an agent's persistent todo list.

## Parameters

- `Index` — 1-based ordinal position used both for display and for addressing the item in updates.
- `Text` — Free-form todo text written by the agent.
- `Completed` — `true` when the agent has marked the item done.

## Properties

### `Completed`

`true` when the agent has marked the item done.

### `Index`

1-based ordinal position used both for display and for addressing the item in updates.

### `Text`

Free-form todo text written by the agent.

## Methods

### `TodoItem`(int Index, string Text, bool Completed)

Single entry in an agent's persistent todo list.

#### Parameters

- `Index` — 1-based ordinal position used both for display and for addressing the item in updates.
- `Text` — Free-form todo text written by the agent.
- `Completed` — `true` when the agent has marked the item done.

### `ToString`

Renders the item in checkbox form (`1. [x] text`) for the agent-facing tool transcript.

