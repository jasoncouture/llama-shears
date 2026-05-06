# LlamaShears.Core.Abstractions.SystemPrompt.WorkspaceFile

Assembly: `LlamaShears.Core.Abstractions`

A single workspace file surfaced to the system-prompt template
([SystemPromptTemplateParameters](SystemPromptTemplateParameters.md).`Files`) so the
template can fold its content directly into the prompt body.

## Parameters

- `Name` — File name relative to the workspace root.
- `Content` — File content as a string.

## Properties

### `Content`

File content as a string.

### `Name`

File name relative to the workspace root.

## Methods

### `WorkspaceFile`(string Name, string Content)

A single workspace file surfaced to the system-prompt template
([SystemPromptTemplateParameters](SystemPromptTemplateParameters.md).`Files`) so the
template can fold its content directly into the prompt body.

#### Parameters

- `Name` — File name relative to the workspace root.
- `Content` — File content as a string.

