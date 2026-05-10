# LlamaShears.Core.Abstractions.Provider.IInferenceRunner

Assembly: `LlamaShears.Core.Abstractions`

Streams a single model prompt, emits per-fragment events, and
optionally emits the resulting Thought / Assistant turn events.
Lifts the inference loop out of the context compactor and the
agent so both can share it; the `eventId` parameter is the
third segment of the published `EventType` and lets
observers tell agent traffic apart from compaction traffic.

## Methods

### `RunAsync`(string eventId, [ILanguageModel](ILanguageModel.md) model, [ModelPrompt](ModelPrompt.md) prompt, [PromptOptions](PromptOptions.md) options, bool emitTurns, Guid correlationId, CancellationToken cancellationToken)

Runs `prompt` through `model`
and publishes message/thought fragment events keyed at
`eventId`. When `emitTurns` is
`true`, also publishes a `Turn(Thought)`
event (if any thinking arrived) and a `Turn(Assistant)`
event (if any content arrived) — callers like the compactor
pass `false` when the produced text is consumed
directly rather than appended to a conversation.

