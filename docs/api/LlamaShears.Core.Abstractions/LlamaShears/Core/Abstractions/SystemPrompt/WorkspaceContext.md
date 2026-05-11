# LlamaShears.Core.Abstractions.SystemPrompt.WorkspaceContext

Assembly: `LlamaShears.Core.Abstractions`

Per-agent workspace overlay: the absolute path the agent reads, writes,
and persists state in, together with the workspace files loaded at scope
initialization. Stashed on the data-context scope under
[WorkspaceContext](WorkspaceContext.md).`DataKey` for template consumption.

## Parameters

- `Path` — Absolute workspace path.
- `Files` — Files loaded from the workspace at scope-init time.

## Fields

### `DataKey`

Key used to stash the active [WorkspaceContext](WorkspaceContext.md) in the per-turn data context scope.

## Properties

### `Files`

Files loaded from the workspace at scope-init time.

### `Path`

Absolute workspace path.

## Methods

### `WorkspaceContext`(string Path, ImmutableArray<[WorkspaceFile](WorkspaceFile.md)> Files)

Per-agent workspace overlay: the absolute path the agent reads, writes,
and persists state in, together with the workspace files loaded at scope
initialization. Stashed on the data-context scope under
[WorkspaceContext](WorkspaceContext.md).`DataKey` for template consumption.

#### Parameters

- `Path` — Absolute workspace path.
- `Files` — Files loaded from the workspace at scope-init time.

