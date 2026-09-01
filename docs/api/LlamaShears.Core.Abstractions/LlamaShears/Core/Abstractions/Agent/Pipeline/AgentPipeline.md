# LlamaShears.Core.Abstractions.Agent.Pipeline.AgentPipeline

Assembly: `LlamaShears.Core.Abstractions`

Folds [IAgentMiddleware](IAgentMiddleware.md) registrations into an onion.
First enumerated step is outermost; last is innermost, wrapping a
no-op terminal.

## Methods

### `AgentPipeline`(IEnumerable<[IAgentMiddleware](IAgentMiddleware.md)> middleware)

Composes `middleware` in enumeration order
(typically DI registration order).

#### Parameters

- `middleware` — Steps to fold. Empty is a no-op pipeline.

### `InvokeAsync`([AgentPipelineContext](AgentPipelineContext.md) context, CancellationToken cancellationToken)

