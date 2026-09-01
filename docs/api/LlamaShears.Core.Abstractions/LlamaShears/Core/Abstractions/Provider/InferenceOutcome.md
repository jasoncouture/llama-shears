# LlamaShears.Core.Abstractions.Provider.InferenceOutcome

Assembly: `LlamaShears.Core.Abstractions`

Aggregated result of one [IInferenceRunner](IInferenceRunner.md).`RunAsync`
pass: the streamed thought/text, any tool calls the model emitted,
and the cumulative token count if the provider reported it.
The runner does not dispatch tools — callers do.

## Parameters

- `Thinking` — Concatenated thought stream (empty when the model produced no thoughts).
- `Content` — Concatenated assistant content (empty when the call only produced tool calls).
- `TokenCount` — Cumulative token count reported via [IModelCompletionResponse](IModelCompletionResponse.md).`TokenCount`; `null` when the provider did not surface one.
- `ToolCalls` — Tool calls the model emitted during this run. Empty when the model produced only text.
- `Interrupted` — `true` when the run terminated because the caller's cancellation token fired; partial fragments and turns were still published.
- `Suppressed` — `true` when the model chose to emit no output for this turn ([Sentinel](../Sentinel.md).`NoResponse`). Distinguishes intentional silence from a transient empty response — callers should not retry on a suppressed turn.

## Properties

### `Content`

Concatenated assistant content (empty when the call only produced tool calls).

### `Interrupted`

`true` when the run terminated because the caller's cancellation token fired; partial fragments and turns were still published.

### `Suppressed`

`true` when the model chose to emit no output for this turn ([Sentinel](../Sentinel.md).`NoResponse`). Distinguishes intentional silence from a transient empty response — callers should not retry on a suppressed turn.

### `Thinking`

Concatenated thought stream (empty when the model produced no thoughts).

### `TokenCount`

Cumulative token count reported via [IModelCompletionResponse](IModelCompletionResponse.md).`TokenCount`; `null` when the provider did not surface one.

### `ToolCalls`

Tool calls the model emitted during this run. Empty when the model produced only text.

## Methods

### `InferenceOutcome`(string Thinking, string Content, Nullable<int> TokenCount, ImmutableArray<[ToolCall](ToolCall.md)> ToolCalls, bool Interrupted, bool Suppressed)

Aggregated result of one [IInferenceRunner](IInferenceRunner.md).`RunAsync`
pass: the streamed thought/text, any tool calls the model emitted,
and the cumulative token count if the provider reported it.
The runner does not dispatch tools — callers do.

#### Parameters

- `Thinking` — Concatenated thought stream (empty when the model produced no thoughts).
- `Content` — Concatenated assistant content (empty when the call only produced tool calls).
- `TokenCount` — Cumulative token count reported via [IModelCompletionResponse](IModelCompletionResponse.md).`TokenCount`; `null` when the provider did not surface one.
- `ToolCalls` — Tool calls the model emitted during this run. Empty when the model produced only text.
- `Interrupted` — `true` when the run terminated because the caller's cancellation token fired; partial fragments and turns were still published.
- `Suppressed` — `true` when the model chose to emit no output for this turn ([Sentinel](../Sentinel.md).`NoResponse`). Distinguishes intentional silence from a transient empty response — callers should not retry on a suppressed turn.

