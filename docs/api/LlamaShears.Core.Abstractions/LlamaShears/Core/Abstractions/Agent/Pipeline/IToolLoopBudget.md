# LlamaShears.Core.Abstractions.Agent.Pipeline.IToolLoopBudget

Assembly: `LlamaShears.Core.Abstractions`

Per-session count of model rounds since the last inbound
user or framework-user batch. Used with
[AgentToolConfig](../AgentToolConfig.md).`TurnLimit` (zero = unlimited).

## Properties

### `IsFinal`

`true` when a positive
[AgentToolConfig](../AgentToolConfig.md).`TurnLimit` has been reached
and this iteration must run without tools.

### `Iteration`

1-based count of model rounds in the current budget window.

## Methods

### `ObserveBatch`(ImmutableArray<[ModelTurn](../../Provider/ModelTurn.md)> batch)

Record an inbound batch. Resets when the batch contains a
[ModelRole](../../Provider/ModelRole.md).`User` or [ModelRole](../../Provider/ModelRole.md).`FrameworkUser`
turn, then increments the iteration counter.

