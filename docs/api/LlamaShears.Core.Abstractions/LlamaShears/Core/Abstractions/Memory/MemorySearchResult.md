# LlamaShears.Core.Abstractions.Memory.MemorySearchResult

Assembly: `LlamaShears.Core.Abstractions`

One hit returned by [IMemorySearcher](IMemorySearcher.md).`SearchAsync`:
where the memory lives, how similar it is to the query, the
first line as a one-shot summary, and the full body. Both the
summary and the body come from a single cached file read so
callers don't need to re-open the file.

## Parameters

- `RelativePath` — Workspace-relative path to the memory file.
- `Score` — Cosine-similarity score, in [0, 1].
- `Summary` — First line of the backing file (typically a markdown H1). Empty when the file has no content.
- `Content` — Full file body. Empty when the file is empty.

## Properties

### `Content`

Full file body. Empty when the file is empty.

### `RelativePath`

Workspace-relative path to the memory file.

### `Score`

Cosine-similarity score, in [0, 1].

### `Summary`

First line of the backing file (typically a markdown H1). Empty when the file has no content.

## Methods

### `MemorySearchResult`(string RelativePath, double Score, string Summary, string Content)

One hit returned by [IMemorySearcher](IMemorySearcher.md).`SearchAsync`:
where the memory lives, how similar it is to the query, the
first line as a one-shot summary, and the full body. Both the
summary and the body come from a single cached file read so
callers don't need to re-open the file.

#### Parameters

- `RelativePath` — Workspace-relative path to the memory file.
- `Score` — Cosine-similarity score, in [0, 1].
- `Summary` — First line of the backing file (typically a markdown H1). Empty when the file has no content.
- `Content` — Full file body. Empty when the file is empty.

