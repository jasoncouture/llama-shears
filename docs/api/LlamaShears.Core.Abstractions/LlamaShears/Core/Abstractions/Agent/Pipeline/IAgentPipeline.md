# LlamaShears.Core.Abstractions.Agent.Pipeline.IAgentPipeline

Assembly: `LlamaShears.Core.Abstractions`

Invokes the registered [IAgentMiddleware](IAgentMiddleware.md) onion for one
dequeued batch. The loop owner builds [AgentPipelineContext](AgentPipelineContext.md)
and calls this once per batch; it does not dequeue, lock, or subscribe.

## Methods

### `InvokeAsync`([AgentPipelineContext](AgentPipelineContext.md) context, CancellationToken cancellationToken)

Runs `context` through every registered
middleware, lowest [IAgentMiddleware](IAgentMiddleware.md).`Order` first.
The terminal is a no-op — the innermost step must perform the
iteration work.

#### Parameters

- `context` — The bag for this batch. Must not be `null`.
- `cancellationToken` — Loop-level cancellation forwarded to every step.

