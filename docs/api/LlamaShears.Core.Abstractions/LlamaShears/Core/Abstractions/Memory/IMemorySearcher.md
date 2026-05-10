# LlamaShears.Core.Abstractions.Memory.IMemorySearcher

Assembly: `LlamaShears.Core.Abstractions`

Vector-search over the agent's memory index. Returns workspace-relative
paths and similarity scores; the agent reads bodies on demand via the
filesystem read-file tool.

## Methods

### `SearchAsync`(string agentId, string query, Nullable<int> limit, Nullable<double> minScore, CancellationToken cancellationToken)

Returns the top hits whose cosine similarity to
`query` meets the score floor, ordered by
descending score. `limit` and
`minScore` default to the agent's
`AgentMemoryConfig` values when left `null`;
callers (e.g. the memory tool) pass explicit overrides when
they need different bounds.

