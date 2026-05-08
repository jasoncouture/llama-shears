# LlamaShears.Core.Abstractions.Paths

Filesystem-path contracts for [LlamaShears](https://github.com/jasoncouture/llama-shears). The host is filesystem-first — every persistent surface (agents directory, workspaces, context, templates) lives under a configurable root. Anything that needs to know "where does X live on disk" goes through `IShearsPaths`.

## Public surface

- **`IShearsPaths`** — `GetPath(PathKind kind)` resolves a logical path to an absolute filesystem path.
- **`PathKind`** — the well-known set: `DataRoot`, `WorkspaceRoot`, `AgentsRoot`, `TemplatesRoot`, `ContextRoot`.

## See also

- [Architecture overview](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/architecture.md)
- [Paths and data layout](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/paths.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.

---

## LlamaShears.Core.Abstractions.Paths

- [FileType](LlamaShears/Core/Abstractions/Paths/FileType.md)
- [IFileProtectionPolicy](LlamaShears/Core/Abstractions/Paths/IFileProtectionPolicy.md)
- [IPathExpander](LlamaShears/Core/Abstractions/Paths/IPathExpander.md)
- [IShearsPaths](LlamaShears/Core/Abstractions/Paths/IShearsPaths.md)
- [PathKind](LlamaShears/Core/Abstractions/Paths/PathKind.md)
- [ProtectedFile](LlamaShears/Core/Abstractions/Paths/ProtectedFile.md)
- [ProtectionMode](LlamaShears/Core/Abstractions/Paths/ProtectionMode.md)

