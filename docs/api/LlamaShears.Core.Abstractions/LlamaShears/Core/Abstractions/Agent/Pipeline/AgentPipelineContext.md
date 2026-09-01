# LlamaShears.Core.Abstractions.Agent.Pipeline.AgentPipelineContext

Assembly: `LlamaShears.Core.Abstractions`

Mutable bag handed to every [IAgentMiddleware](IAgentMiddleware.md) for one
dequeued batch. The loop owner constructs it; middleware fill
[AgentPipelineContext](AgentPipelineContext.md).`CorrelationId`, [AgentPipelineContext](AgentPipelineContext.md).`TurnToken`, and
[AgentPipelineContext](AgentPipelineContext.md).`Outcome` as they run.

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

### `Outcome`

Result of the iteration runner. `null` until
the run-iteration step completes (or the chain short-circuits).

### `ShutdownToken`

Loop-level cancellation. Outlives a turn interrupt — tail
persistence that must finish after interrupt uses this, not
[AgentPipelineContext](AgentPipelineContext.md).`TurnToken`.

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

