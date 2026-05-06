# LlamaShears.Core.Abstractions.Provider.ILanguageModel

Assembly: `LlamaShears.Core.Abstractions.Provider`

Provider-agnostic seam for invoking a language model. Implementations
own the wire format and lifecycle of the underlying model; callers
see only the streaming response of [IModelResponseFragment](IModelResponseFragment.md)
values.

## Methods

### `EstimateAsync`([ModelTurn](ModelTurn.md) turn, CancellationToken cancellationToken)

Returns an upper-bound token estimate for `turn`.
Implementations that can reach a real tokenizer should override
to return a tight count (still favoring over-estimation when the
chat-template wrap is unknown). The default is a coarse
character-based heuristic intended only as a safe fallback —
never returns less than the actual cost.

### `PromptAsync`([ModelPrompt](ModelPrompt.md) prompt, [PromptOptions](PromptOptions.md) options, CancellationToken cancellationToken)

Streams the model's response to `prompt` as a
sequence of fragments. `options` overrides the
model's configured defaults for this single call (null = use
configuration). The enumeration completes when the model has
finished; cancellation aborts the in-flight request. The final
fragment is always an [IModelCompletionResponse](IModelCompletionResponse.md) when
the provider can report token usage.

