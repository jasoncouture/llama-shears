# LlamaShears.Core.Abstractions.PromptContext.PromptContextMemory

Assembly: `LlamaShears.Core.Abstractions.PromptContext`

One memory hit surfaced to the per-turn prompt-context template
([IPromptContextProvider](IPromptContextProvider.md)). The agent reads the body
from disk via the read-file tool when it actually wants the
content; the template only sees the summary and score.

## Parameters

- `RelativePath` — Workspace-relative path to the memory file.
- `Summary` — Short summary line surfaced to the model.
- `Score` — Cosine-similarity score against the search query, in [0, 1].

## Properties

### `RelativePath`

Workspace-relative path to the memory file.

### `Score`

Cosine-similarity score against the search query, in [0, 1].

### `Summary`

Short summary line surfaced to the model.

## Methods

### `PromptContextMemory`(string RelativePath, string Summary, double Score)

One memory hit surfaced to the per-turn prompt-context template
([IPromptContextProvider](IPromptContextProvider.md)). The agent reads the body
from disk via the read-file tool when it actually wants the
content; the template only sees the summary and score.

#### Parameters

- `RelativePath` — Workspace-relative path to the memory file.
- `Summary` — Short summary line surfaced to the model.
- `Score` — Cosine-similarity score against the search query, in [0, 1].

