# LlamaShears.Core.Abstractions.Provider.IInferenceRunner

Assembly: `LlamaShears.Core.Abstractions`

Streams a single model prompt, emits per-fragment events, and
optionally emits the resulting Thought / Assistant turn events.
Lifts the inference loop out of the context compactor and the
agent so both can share it; the language model, event-id, and
correlation-id used for the call and the published events are all
resolved from the ambient agent scope, so callers set state once
on the data scope before invoking the runner instead of threading
it through every call.

## Methods

### `RunAsync`([ModelPrompt](ModelPrompt.md) prompt, [PromptOptions](PromptOptions.md) options, CancellationToken cancellationToken)

Runs `prompt` through the scope's language
model and publishes message/thought fragment events keyed at
the ambient agent state's event id. When
[PromptOptions](PromptOptions.md).`EmitTurns` is `true`,
also publishes a `Turn(Thought)` event (if any thinking
arrived) and a `Turn(Assistant)` event (if any content
arrived) — callers like the compactor leave it at
`false` when the produced text is consumed
directly rather than appended to a conversation.

