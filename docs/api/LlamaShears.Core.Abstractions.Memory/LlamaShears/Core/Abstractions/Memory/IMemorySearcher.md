# LlamaShears.Core.Abstractions.Memory.IMemorySearcher

Assembly: `LlamaShears.Core.Abstractions.Memory`

Vector-search over the agent's memory index. Returns workspace-relative
paths and similarity scores; the agent reads bodies on demand via the
filesystem read-file tool.

## Methods

### `SearchAsync`(string agentId, string query, int limit, double minScore, CancellationToken cancellationToken)

Returns the top `limit` hits whose cosine
similarity to `query` is at least
`minScore`, ordered by descending score.

