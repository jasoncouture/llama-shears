# LlamaShears.Core.Abstractions.Memory.MemoryRef

Assembly: `LlamaShears.Core.Abstractions.Memory`

Lightweight reference to a memory file written via
[IMemoryStore](IMemoryStore.md).`StoreAsync`. Workspace-relative path only —
the agent reads the body on demand.

## Parameters

- `RelativePath` — Workspace-relative path to the memory file.

## Properties

### `RelativePath`

Workspace-relative path to the memory file.

## Methods

### `MemoryRef`(string RelativePath)

Lightweight reference to a memory file written via
[IMemoryStore](IMemoryStore.md).`StoreAsync`. Workspace-relative path only —
the agent reads the body on demand.

#### Parameters

- `RelativePath` — Workspace-relative path to the memory file.

