# LlamaShears.Core.Abstractions.Agent.IterationOutcome

Assembly: `LlamaShears.Core.Abstractions`

Result of running one agent iteration: was the turn interrupted before
completion, and any tool-result turns the inference produced that the
caller should feed back into its driver on the next iteration.

## Parameters

- `Interrupted` — `true` when the turn cancellation token tripped before
the inference finished; partial output may have been published but the
caller should not act on tool results in that case.
- `ToolResultTurns` — One [ModelTurn](../Provider/ModelTurn.md) per dispatched tool call. Empty when the
model emitted no tool calls (the natural exit condition).

## Properties

### `Interrupted`

`true` when the turn cancellation token tripped before
the inference finished; partial output may have been published but the
caller should not act on tool results in that case.

### `ToolResultTurns`

One [ModelTurn](../Provider/ModelTurn.md) per dispatched tool call. Empty when the
model emitted no tool calls (the natural exit condition).

## Methods

### `IterationOutcome`(bool Interrupted, ImmutableArray<[ModelTurn](../Provider/ModelTurn.md)> ToolResultTurns)

Result of running one agent iteration: was the turn interrupted before
completion, and any tool-result turns the inference produced that the
caller should feed back into its driver on the next iteration.

#### Parameters

- `Interrupted` — `true` when the turn cancellation token tripped before
the inference finished; partial output may have been published but the
caller should not act on tool results in that case.
- `ToolResultTurns` — One [ModelTurn](../Provider/ModelTurn.md) per dispatched tool call. Empty when the
model emitted no tool calls (the natural exit condition).

