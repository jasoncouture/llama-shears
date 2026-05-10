# LlamaShears.Core.Abstractions.Agent.Todo

## Types

- [ITodoStorage](ITodoStorage.md) — Persists the agent's TODO list as a Markdown file at the workspace root. All mutations rewrite or append to that file; a corrupt file is reset to the canonical empty state and the result reflects that recovery.
- [TodoCommandResult](TodoCommandResult.md) — Outcome of a todo-command invocation: the resulting items, an overall state code, and an optional human-facing message that explains why the command failed or why the list was rebuilt.
- [TodoItem](TodoItem.md) — Single entry in an agent's persistent todo list.
- [TodoItemUpdate](TodoItemUpdate.md) — Patch applied to a single [TodoItem](TodoItem.md) when the agent toggles its completion state.
- [TodoResultState](TodoResultState.md) — Overall outcome reported by a todo command.

