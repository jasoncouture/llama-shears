# LlamaShears.Core.Abstractions.Agent.IAgentFactory

Assembly: `LlamaShears.Core.Abstractions`

Surfaces the catalog of agents a host knows about and constructs
[IAgent](IAgent.md) instances from [AgentConfiguration](AgentConfiguration.md).
Implementations decide where the catalog comes from (disk, registry,
in-memory) and what construction means (DI activation, plugin
resolution).

## Methods

### `CreateAgent`([AgentConfiguration](AgentConfiguration.md) configuration)

Creates an agent instance from `configuration`.

### `ListAgentsAsync`(CancellationToken cancellationToken)

Lists every agent the factory surfaces, with metadata.

