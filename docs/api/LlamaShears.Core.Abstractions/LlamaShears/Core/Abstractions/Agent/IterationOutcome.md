# LlamaShears.Core.Abstractions.Agent.IterationOutcome

Assembly: `LlamaShears.Core.Abstractions`

Result of running one agent iteration: was the turn interrupted before
completion, the tool calls the model emitted, and any tool-result
turns dispatch middleware produced for the next iteration.

## Parameters

- `Interrupted` — `true` when the turn cancellation token tripped before
the inference finished; partial output may have been published.
Dispatch middleware still runs so in-flight calls pair with error
results; the enqueue step does not feed those back.
- `ToolResultTurns` — One [ModelTurn](../Provider/ModelTurn.md) per dispatched tool call. Empty until
dispatch middleware runs, or when the model emitted no tool calls.
- `ToolCalls` — Tool calls the model emitted. Empty when the model produced only text.
Dispatch middleware reads this; the iteration runner does not execute them.

## Properties

### `Interrupted`

`true` when the turn cancellation token tripped before
the inference finished; partial output may have been published.
Dispatch middleware still runs so in-flight calls pair with error
results; the enqueue step does not feed those back.

### `ToolCalls`

Tool calls the model emitted. Empty when the model produced only text.
Dispatch middleware reads this; the iteration runner does not execute them.

### `ToolResultTurns`

One [ModelTurn](../Provider/ModelTurn.md) per dispatched tool call. Empty until
dispatch middleware runs, or when the model emitted no tool calls.

## Methods

### `IterationOutcome`(bool Interrupted, ImmutableArray<[ModelTurn](../Provider/ModelTurn.md)> ToolResultTurns, ImmutableArray<[ToolCall](../Provider/ToolCall.md)> ToolCalls)

Result of running one agent iteration: was the turn interrupted before
completion, the tool calls the model emitted, and any tool-result
turns dispatch middleware produced for the next iteration.

#### Parameters

- `Interrupted` — `true` when the turn cancellation token tripped before
the inference finished; partial output may have been published.
Dispatch middleware still runs so in-flight calls pair with error
results; the enqueue step does not feed those back.
- `ToolResultTurns` — One [ModelTurn](../Provider/ModelTurn.md) per dispatched tool call. Empty until
dispatch middleware runs, or when the model emitted no tool calls.
- `ToolCalls` — Tool calls the model emitted. Empty when the model produced only text.
Dispatch middleware reads this; the iteration runner does not execute them.

