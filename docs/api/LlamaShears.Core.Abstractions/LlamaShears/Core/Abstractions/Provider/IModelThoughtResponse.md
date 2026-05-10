# LlamaShears.Core.Abstractions.Provider.IModelThoughtResponse

Assembly: `LlamaShears.Core.Abstractions`

Streaming fragment carrying hidden reasoning from a thinking-capable
model. Recorded for visibility but never resubmitted as part of a
later prompt.

## Properties

### `Content`

Reasoning content streamed by a thinking-capable model. Thoughts
are produced separately from the user-facing response and are
kept out of subsequent prompts — providers must filter
[ModelRole](ModelRole.md).`Thought` turns when sending context back
to the model.

