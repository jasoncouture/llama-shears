# LlamaShears.Core.Abstractions.SystemPrompt

System-prompt and template-rendering contracts for [LlamaShears](https://github.com/jasoncouture/llama-shears). The framework drives system prompts through Scriban templates with a fallback chain (per-agent → bundled default); the contracts here keep that pipeline pluggable.

## Public surface

- **`ISystemPromptProvider`** — produces the rendered system prompt for an agent at a given moment.
- **`ITemplateRenderer`** — the template-language abstraction; the shipping implementation is Scriban, but callers see only the rendered string.
- **`SystemPromptTemplateParameters`** — the value bag the system-prompt template binds against (workspace files, agent metadata, etc.).
- **`WorkspaceFile`** — the typed handle templates use to read conventional workspace files (`BOOTSTRAP.md`, `IDENTITY.md`, `SOUL.md`, `USER.md`, `TOOLS.md`, `MEMORY.md`).

## See also

- [System prompts and prompt context](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/prompt-context.md)
- [Agent workspace](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/agent-workspace.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.

---

## LlamaShears.Core.Abstractions.SystemPrompt

- [ISystemPromptProvider](LlamaShears/Core/Abstractions/SystemPrompt/ISystemPromptProvider.md)
- [ITemplateFileLocator](LlamaShears/Core/Abstractions/SystemPrompt/ITemplateFileLocator.md)
- [ITemplateRenderer](LlamaShears/Core/Abstractions/SystemPrompt/ITemplateRenderer.md)
- [SystemPromptTemplateParameters](LlamaShears/Core/Abstractions/SystemPrompt/SystemPromptTemplateParameters.md)
- [WorkspaceFile](LlamaShears/Core/Abstractions/SystemPrompt/WorkspaceFile.md)

