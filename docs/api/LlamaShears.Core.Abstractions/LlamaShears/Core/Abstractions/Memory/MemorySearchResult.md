# LlamaShears.Core.Abstractions.Memory.MemorySearchResult

Assembly: `LlamaShears.Core.Abstractions`

One hit returned by [IMemorySearcher](IMemorySearcher.md).`SearchAsync`:
where the memory lives and how similar it is to the query.

## Parameters

- `RelativePath` — Workspace-relative path to the memory file.
- `Score` — Cosine-similarity score, in [0, 1].

## Properties

### `RelativePath`

Workspace-relative path to the memory file.

### `Score`

Cosine-similarity score, in [0, 1].

## Methods

### `MemorySearchResult`(string RelativePath, double Score)

One hit returned by [IMemorySearcher](IMemorySearcher.md).`SearchAsync`:
where the memory lives and how similar it is to the query.

#### Parameters

- `RelativePath` — Workspace-relative path to the memory file.
- `Score` — Cosine-similarity score, in [0, 1].

