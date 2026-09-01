# LlamaShears.Core.Abstractions.Provider.IInferenceRunner

Assembly: `LlamaShears.Core.Abstractions`

Streams a single model prompt, emits per-fragment events, and
optionally emits the resulting Thought / Assistant turn events.
Lifts the inference loop out of the context compactor and the
agent so both can share it. Callers pass the session and
correlation used to key published events; the runner does not
read them from the ambient data scope.

## Methods

### `RunAsync`([ModelPrompt](ModelPrompt.md) prompt, [PromptOptions](PromptOptions.md) options, [SessionId](../Agent/Sessions/SessionId.md) sessionId, Guid correlationId, CancellationToken cancellationToken)

Runs `prompt` through the scope's language
model and publishes message/thought fragment events keyed at
`sessionId`. When
[PromptOptions](PromptOptions.md).`EmitTurns` is `true`,
also publishes a `Turn(Thought)` event (if any thinking
arrived) and a `Turn(Assistant)` event (if any content
arrived) — callers like the compactor leave it at
`false` when the produced text is consumed
directly rather than appended to a conversation.

