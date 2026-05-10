# LlamaShears.Core.Abstractions.SystemPrompt.WorkspaceFile

Assembly: `LlamaShears.Core.Abstractions`

In-memory representation of a single file that should land in an agent's
workspace overlay alongside the rendered system prompt.

## Parameters

- `Name` — Workspace-relative file name (including extension and any subdirectories).
- `Content` — UTF-8 text content of the file.

## Properties

### `Content`

UTF-8 text content of the file.

### `Name`

Workspace-relative file name (including extension and any subdirectories).

## Methods

### `WorkspaceFile`(string Name, string Content)

In-memory representation of a single file that should land in an agent's
workspace overlay alongside the rendered system prompt.

#### Parameters

- `Name` — Workspace-relative file name (including extension and any subdirectories).
- `Content` — UTF-8 text content of the file.

