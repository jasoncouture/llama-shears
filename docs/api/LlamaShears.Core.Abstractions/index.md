# LlamaShears.Core.Abstractions

Convenience metapackage that pulls in every `LlamaShears.Core.Abstractions.*` sub-package as a project / NuGet reference. Take this when you want the entire contract surface a plugin or third-party consumer might need; take the individual sub-packages directly when you want a narrower dependency footprint.

## What this contains

This package ships no types of its own. It re-exports the following sub-packages via `<ProjectReference>` (or NuGet dependency, when consumed as a package):

- `LlamaShears.Core.Abstractions.Agent`
- `LlamaShears.Core.Abstractions.Caching`
- `LlamaShears.Core.Abstractions.Content`
- `LlamaShears.Core.Abstractions.Context`
- `LlamaShears.Core.Abstractions.Events`
- `LlamaShears.Core.Abstractions.Memory`
- `LlamaShears.Core.Abstractions.Paths`
- `LlamaShears.Core.Abstractions.PromptContext`
- `LlamaShears.Core.Abstractions.Provider`
- `LlamaShears.Core.Abstractions.SystemPrompt`

## See also

- [Architecture overview](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/architecture.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.

---

