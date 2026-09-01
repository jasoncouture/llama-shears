# LlamaShears.Core.Abstractions.Agent.Pipeline.IAgentMiddleware

Assembly: `LlamaShears.Core.Abstractions`

One step in the per-batch agent onion. Implementations do work
before and/or after invoking the next delegate; skipping it
short-circuits the rest of the chain. Each implementation owns
exactly one concern.

## Methods

### `InvokeAsync`([AgentPipelineContext](AgentPipelineContext.md) context, [AgentMiddlewareDelegate](AgentMiddlewareDelegate.md) next, CancellationToken cancellationToken)

Runs this step around the remainder of the pipeline.

#### Parameters

- `context` — The mutable bag for this dequeued batch.
- `next` — The rest of the chain. Must be invoked unless this step short-circuits.
- `cancellationToken` — Loop-level cancellation. Per-turn interrupt is [AgentPipelineContext](AgentPipelineContext.md).`TurnToken`.

