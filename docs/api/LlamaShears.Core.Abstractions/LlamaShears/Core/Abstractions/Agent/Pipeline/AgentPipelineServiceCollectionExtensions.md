# LlamaShears.Core.Abstractions.Agent.Pipeline.AgentPipelineServiceCollectionExtensions

Assembly: `LlamaShears.Core.Abstractions`

DI helpers for the per-batch agent middleware onion.

## Methods

### `AddAgentPipeline`(IServiceCollection services)

Registers the [IAgentPipeline](IAgentPipeline.md) fold. Idempotent.
Called automatically by [AgentPipelineServiceCollectionExtensions](AgentPipelineServiceCollectionExtensions.md).`AddAgentMiddleware``1`.

#### Parameters

- `services` — The collection to add to.

#### Returns

`services` for chaining.

