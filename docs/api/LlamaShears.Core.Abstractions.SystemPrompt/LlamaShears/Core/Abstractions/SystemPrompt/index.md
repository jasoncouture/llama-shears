# LlamaShears.Core.Abstractions.SystemPrompt

## Types

- [ISystemPromptProvider](ISystemPromptProvider.md) — Resolves a named system prompt template, renders it against [SystemPromptTemplateParameters](SystemPromptTemplateParameters.md), and returns the body to feed into the model's system turn. Bodies are stable for the agent's lifetime so the model's prompt-cache prefix stays warm across turns.
- [ITemplateRenderer](ITemplateRenderer.md) — Renders a template file against an input object. Implementations own the template language (today: Scriban); callers see only the rendered string.
- [SystemPromptTemplateParameters](SystemPromptTemplateParameters.md) — Inputs the system-prompt template has access to when rendered by [ISystemPromptProvider](ISystemPromptProvider.md). Templates are Scriban; new values are added here rather than composed in C# so the template stays the single point of authorship.
- [WorkspaceFile](WorkspaceFile.md) — A single workspace file surfaced to the system-prompt template ([SystemPromptTemplateParameters](SystemPromptTemplateParameters.md).`Files`) so the template can fold its content directly into the prompt body.

