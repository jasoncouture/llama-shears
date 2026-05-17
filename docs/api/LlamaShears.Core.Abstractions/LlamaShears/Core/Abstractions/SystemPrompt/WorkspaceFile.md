# LlamaShears.Core.Abstractions.SystemPrompt.WorkspaceFile

Assembly: `LlamaShears.Core.Abstractions`

In-memory representation of a single file that should land in an agent's
workspace overlay alongside the rendered system prompt.

## Parameters

- `Name` — Leaf file name (e.g. `AGENTS.md`).
- `Path` — Absolute directory the file lives in, terminated by the platform directory separator (e.g. `/home/user/.llama-shears/workspace/alpha/`). Concatenating `Path` + `Name` yields the file's absolute path.
- `Content` — UTF-8 text content of the file.

## Properties

### `Content`

UTF-8 text content of the file.

### `Name`

Leaf file name (e.g. `AGENTS.md`).

### `Path`

Absolute directory the file lives in, terminated by the platform directory separator (e.g. `/home/user/.llama-shears/workspace/alpha/`). Concatenating `Path` + `Name` yields the file's absolute path.

## Methods

### `WorkspaceFile`(string Name, string Path, string Content)

In-memory representation of a single file that should land in an agent's
workspace overlay alongside the rendered system prompt.

#### Parameters

- `Name` — Leaf file name (e.g. `AGENTS.md`).
- `Path` — Absolute directory the file lives in, terminated by the platform directory separator (e.g. `/home/user/.llama-shears/workspace/alpha/`). Concatenating `Path` + `Name` yields the file's absolute path.
- `Content` — UTF-8 text content of the file.

