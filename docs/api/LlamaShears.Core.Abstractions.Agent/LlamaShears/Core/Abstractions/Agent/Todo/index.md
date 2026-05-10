# LlamaShears.Core.Abstractions.Agent.Todo

## Types

- [ITodoStorage](ITodoStorage.md) — Persists the agent's TODO list as a Markdown file at the workspace root. All mutations rewrite or append to that file; a corrupt file is reset to the canonical empty state and the result reflects that recovery.

