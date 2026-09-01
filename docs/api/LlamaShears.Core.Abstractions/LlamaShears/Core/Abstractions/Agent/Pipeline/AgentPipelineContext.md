# LlamaShears.Core.Abstractions.Agent.Pipeline.AgentPipelineContext

Assembly: `LlamaShears.Core.Abstractions`

Mutable bag handed to every [IAgentMiddleware](IAgentMiddleware.md) for one
dequeued batch. The loop owner constructs it; middleware fill
[AgentPipelineContext](AgentPipelineContext.md).`CorrelationId`, [AgentPipelineContext](AgentPipelineContext.md).`TurnToken`,
[AgentPipelineContext](AgentPipelineContext.md).`SystemPrompt`, [AgentPipelineContext](AgentPipelineContext.md).`EphemeralContext`,
[AgentPipelineContext](AgentPipelineContext.md).`Prompt`, and [AgentPipelineContext](AgentPipelineContext.md).`Outcome` as they run.

## Properties

### `AgentContext`

Live persisted conversation for the session. Token metrics and
any turn the iteration persists land here.

### `Batch`

Inbound turns for this iteration. Middleware may replace the
array (filter, coalesce) before the iteration runner sees it.

### `CorrelationId`

Correlation id stamped on events published during the
iteration. Empty until correlation-scope middleware assigns one.

### `EphemeralContext`

Per-turn prompt-context turn for this batch. `null`
until ephemeral-context middleware renders it, or when the
template is empty. Not persisted; that middleware also inserts
it into [AgentPipelineContext](AgentPipelineContext.md).`Prompt` immediately before the last user
cluster when the compacted prompt ends in a user turn.

### `Outcome`

Result of the iteration runner. `null` until
the run-iteration step completes (or the chain short-circuits).

### `Prompt`

Model prompt for this batch. `null` until
compaction middleware builds it (and possibly rewrites it).
Ephemeral-context middleware may then insert
[AgentPipelineContext](AgentPipelineContext.md).`EphemeralContext`. The iteration sends this as-is.

### `ShutdownToken`

Loop-level cancellation. Outlives a turn interrupt — tail
persistence that must finish after interrupt uses this, not
[AgentPipelineContext](AgentPipelineContext.md).`TurnToken`.

### `SystemPrompt`

Persistent system-prompt turn for this batch. `null`
until system-prompt middleware renders it. Not persisted; compaction
prepends it when building [AgentPipelineContext](AgentPipelineContext.md).`Prompt`.

### `TurnToken`

Cancellation linked to interrupt signals. Defaults to
[AgentPipelineContext](AgentPipelineContext.md).`ShutdownToken` until interrupt-scope middleware
installs a linked source.

## Methods

### `AgentPipelineContext`([IAgentContext](../Persistence/IAgentContext.md) agentContext, ImmutableArray<[ModelTurn](../../Provider/ModelTurn.md)> batch, CancellationToken shutdownToken)

Builds a context for `batch`.
[AgentPipelineContext](AgentPipelineContext.md).`TurnToken` starts as `shutdownToken`
so steps that run before interrupt-scope wiring still have a token.

#### Parameters

- `agentContext` — Live persisted conversation for the session being driven.
- `batch` — Inbound turns dequeued for this iteration (user and/or tool).
- `shutdownToken` — Loop-level cancellation; also the initial [AgentPipelineContext](AgentPipelineContext.md).`TurnToken`.

