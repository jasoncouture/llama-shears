# LlamaShears.Core.Abstractions

The full contract surface that plugins and third-party consumers compile against. Take this when you want every public abstraction LlamaShears exposes — every interface, DTO, and shared type lives in this single package.

## What this contains

The package is organised into namespaces by concern; each namespace was previously its own `LlamaShears.Core.Abstractions.*` sub-package and has been collapsed into this assembly:

- `LlamaShears.Core.Abstractions.Agent` — agent identity, configuration, lifecycle, persistence, sessions, todos.
- `LlamaShears.Core.Abstractions.Caching` — file-parser cache and shears cache contracts.
- `LlamaShears.Core.Abstractions.Commands` — slash-command registry and contracts.
- `LlamaShears.Core.Abstractions.Common` — data-context primitives shared across the surface.
- `LlamaShears.Core.Abstractions.Content` — attachment + content kinds.
- `LlamaShears.Core.Abstractions.Context` — agent / language-model / system / tool / plugin context plus compaction.
- `LlamaShears.Core.Abstractions.Events` — event bus, envelopes, filters, delivery modes, agent + channel messages.
- `LlamaShears.Core.Abstractions.Memory` — memory store/indexer/searcher contracts and reconciliation types.
- `LlamaShears.Core.Abstractions.Paths` — `IShearsPaths`, file-protection policy, expansion contracts.
- `LlamaShears.Core.Abstractions.PromptContext` — prompt context provider + memory.
- `LlamaShears.Core.Abstractions.Provider` — language-model / embedding provider factories, prompts, turns, tool descriptors, model identity.
- `LlamaShears.Core.Abstractions.SystemPrompt` — system-prompt provider, template renderer, template file locator, workspace files.

## See also

- [Architecture overview](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/architecture.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.
