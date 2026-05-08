# LlamaShears.Core.Abstractions.SystemPrompt.SystemPromptTemplateParameters

Assembly: `LlamaShears.Core.Abstractions.SystemPrompt`

Inputs the system-prompt template has access to when rendered by
[ISystemPromptProvider](ISystemPromptProvider.md). Templates are Scriban; new
values are added here rather than composed in C# so the template
stays the single point of authorship.

## Parameters

- `AgentId` — Identifier of the agent the prompt is being rendered for; `null` when not bound to a specific agent.
- `WorkspacePath` — Absolute workspace path; `null` when not bound to a workspace.

## Properties

### `AgentId`

Identifier of the agent the prompt is being rendered for; `null` when not bound to a specific agent.

### `Files`

Workspace files surfaced to the template (e.g. `AGENTS.md`, agent-specific manifests).

### `WorkspacePath`

Absolute workspace path; `null` when not bound to a workspace.

## Methods

### `SystemPromptTemplateParameters`(string AgentId, string WorkspacePath)

Inputs the system-prompt template has access to when rendered by
[ISystemPromptProvider](ISystemPromptProvider.md). Templates are Scriban; new
values are added here rather than composed in C# so the template
stays the single point of authorship.

#### Parameters

- `AgentId` — Identifier of the agent the prompt is being rendered for; `null` when not bound to a specific agent.
- `WorkspacePath` — Absolute workspace path; `null` when not bound to a workspace.

