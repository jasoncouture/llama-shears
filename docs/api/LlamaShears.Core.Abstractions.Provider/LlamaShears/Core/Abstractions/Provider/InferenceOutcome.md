# LlamaShears.Core.Abstractions.Provider.InferenceOutcome

Assembly: `LlamaShears.Core.Abstractions.Provider`

Aggregated result of one [IInferenceRunner](IInferenceRunner.md).`RunAsync`
pass: the streamed thought/text, any tool calls and their replies,
and the cumulative token count if the provider reported it.

## Parameters

- `Thinking` — Concatenated thought stream (empty when the model produced no thoughts).
- `Content` — Concatenated assistant content (empty when the call only produced tool calls).
- `TokenCount` — Cumulative token count reported via [IModelCompletionResponse](IModelCompletionResponse.md).`TokenCount`; `null` when the provider did not surface one.
- `ToolCalls` — Tool calls the model emitted during this run.
- `ToolResults` — Results of dispatching `ToolCalls`; aligned by index with `ToolCalls`.

## Properties

### `Content`

Concatenated assistant content (empty when the call only produced tool calls).

### `Thinking`

Concatenated thought stream (empty when the model produced no thoughts).

### `TokenCount`

Cumulative token count reported via [IModelCompletionResponse](IModelCompletionResponse.md).`TokenCount`; `null` when the provider did not surface one.

### `ToolCalls`

Tool calls the model emitted during this run.

### `ToolResults`

Results of dispatching `ToolCalls`; aligned by index with `ToolCalls`.

## Methods

### `InferenceOutcome`(string Thinking, string Content, Nullable<int> TokenCount, ImmutableArray<[ToolCall](ToolCall.md)> ToolCalls, ImmutableArray<[ToolCallResult](ToolCallResult.md)> ToolResults)

Aggregated result of one [IInferenceRunner](IInferenceRunner.md).`RunAsync`
pass: the streamed thought/text, any tool calls and their replies,
and the cumulative token count if the provider reported it.

#### Parameters

- `Thinking` — Concatenated thought stream (empty when the model produced no thoughts).
- `Content` — Concatenated assistant content (empty when the call only produced tool calls).
- `TokenCount` — Cumulative token count reported via [IModelCompletionResponse](IModelCompletionResponse.md).`TokenCount`; `null` when the provider did not surface one.
- `ToolCalls` — Tool calls the model emitted during this run.
- `ToolResults` — Results of dispatching `ToolCalls`; aligned by index with `ToolCalls`.

