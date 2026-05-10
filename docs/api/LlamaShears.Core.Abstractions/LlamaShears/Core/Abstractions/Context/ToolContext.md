# LlamaShears.Core.Abstractions.Context.ToolContext

Assembly: `LlamaShears.Core.Abstractions`

The flat tool catalog visible to the agent on an
[AgentContext](AgentContext.md) snapshot. The grouped form
([ToolGroup](../Provider/ToolGroup.md)) is for prompts; this flat form is what
templates and tools iterate over.

## Parameters

- `Items` — Tools available, in registration order.

## Properties

### `Items`

Tools available, in registration order.

## Methods

### `ToolContext`(ImmutableArray<[ToolDescriptor](../Provider/ToolDescriptor.md)> Items)

The flat tool catalog visible to the agent on an
[AgentContext](AgentContext.md) snapshot. The grouped form
([ToolGroup](../Provider/ToolGroup.md)) is for prompts; this flat form is what
templates and tools iterate over.

#### Parameters

- `Items` — Tools available, in registration order.

