# LlamaShears.Core.Abstractions.Agent.Pipeline.AgentPipeline

Assembly: `LlamaShears.Core.Abstractions`

Folds [IAgentMiddleware](IAgentMiddleware.md) into an onion by
[IAgentMiddleware](IAgentMiddleware.md).`Order`. Lowest is outermost; last
after the sort is innermost, wrapping a no-op terminal. Equal
orders keep their original enumeration order.

## Methods

### `AgentPipeline`(IEnumerable<[IAgentMiddleware](IAgentMiddleware.md)> middleware)

Composes `middleware` by
[IAgentMiddleware](IAgentMiddleware.md).`Order` (lowest outermost).

#### Parameters

- `middleware` — Steps to fold. Empty is a no-op pipeline.

### `InvokeAsync`([AgentPipelineContext](AgentPipelineContext.md) context, CancellationToken cancellationToken)

