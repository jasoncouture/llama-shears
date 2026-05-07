# LlamaShears.Core.Abstractions.PromptContext

Per-turn ephemeral prompt-context contracts for [LlamaShears](https://github.com/jasoncouture/llama-shears). The framework injects an "ephemeral" block ahead of each user turn that carries dynamic values (matched memories, current time, attachment metadata, …) without polluting the durable conversation history. This package owns the contracts; rendering is template-driven via `LlamaShears.Core.Abstractions.SystemPrompt.ITemplateRenderer`.

## Public surface

- **`IPromptContextProvider`** — produces the per-turn `PromptContextParameters` an agent's template binds against.
- **`PromptContextParameters`** — the parameter bundle (memories, files, time, etc.) the template sees.
- **`PromptContextMemory`** — the trimmed view of a matched memory the model receives (path + first-line summary, not full body).

## See also

- [System prompts and prompt context](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/prompt-context.md)
- [Memory and RAG](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/memory.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.
