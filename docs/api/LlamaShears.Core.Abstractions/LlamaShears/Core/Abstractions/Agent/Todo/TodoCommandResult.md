# LlamaShears.Core.Abstractions.Agent.Todo.TodoCommandResult

Assembly: `LlamaShears.Core.Abstractions`

Outcome of a todo-command invocation: the resulting items, an overall
state code, and an optional human-facing message that explains why the
command failed or why the list was rebuilt.

## Parameters

- `Items` — Todo items as they stand after the command applied.
- `State` — Overall outcome state — success, recovered-from-corruption, or refusal.
- `Message` — Free-form explanation surfaced alongside non-success states; `null` on plain success.

## Properties

### `Items`

Todo items as they stand after the command applied.

### `Message`

Free-form explanation surfaced alongside non-success states; `null` on plain success.

### `State`

Overall outcome state — success, recovered-from-corruption, or refusal.

## Methods

### `TodoCommandResult`(ImmutableArray<[TodoItem](TodoItem.md)> Items, [TodoResultState](TodoResultState.md) State, string Message)

Outcome of a todo-command invocation: the resulting items, an overall
state code, and an optional human-facing message that explains why the
command failed or why the list was rebuilt.

#### Parameters

- `Items` — Todo items as they stand after the command applied.
- `State` — Overall outcome state — success, recovered-from-corruption, or refusal.
- `Message` — Free-form explanation surfaced alongside non-success states; `null` on plain success.

### `ToString`

Renders the result as the agent-facing string the tool returns: status prefix (when non-success) followed by the ordered todo list.

