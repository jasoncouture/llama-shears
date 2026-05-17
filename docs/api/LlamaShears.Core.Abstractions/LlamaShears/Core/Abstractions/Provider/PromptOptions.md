# LlamaShears.Core.Abstractions.Provider.PromptOptions

Assembly: `LlamaShears.Core.Abstractions`

Per-call overrides passed to [ILanguageModel](ILanguageModel.md).`PromptAsync`.
`null` options means "use the model's configured
defaults verbatim".

## Parameters

- `TokenLimit` — Maximum response tokens for this call; `null` = use the configured limit.
- `Tools` — Tool groups visible to the model for this call; default = no tools.
- `InjectEphemeralContext` — When `true`, the inference runner renders the per-turn prompt-context template and inserts the resulting ephemeral turn into the prompt before dispatch; defaults to `false` so callers that want raw inference (e.g. compaction) keep their current behavior.
- `EmitTurns` — When `true`, the inference runner publishes the resulting Thought / Assistant `Turn` events after the stream completes; `false` for callers that consume the produced text directly without appending it to a conversation (e.g. compaction).

## Properties

### `EmitTurns`

When `true`, the inference runner publishes the resulting Thought / Assistant `Turn` events after the stream completes; `false` for callers that consume the produced text directly without appending it to a conversation (e.g. compaction).

### `InjectEphemeralContext`

When `true`, the inference runner renders the per-turn prompt-context template and inserts the resulting ephemeral turn into the prompt before dispatch; defaults to `false` so callers that want raw inference (e.g. compaction) keep their current behavior.

### `TokenLimit`

Maximum response tokens for this call; `null` = use the configured limit.

### `Tools`

Tool groups visible to the model for this call; default = no tools.

## Methods

### `PromptOptions`(Nullable<int> TokenLimit, ImmutableArray<[ToolGroup](ToolGroup.md)> Tools, bool InjectEphemeralContext, bool EmitTurns)

Per-call overrides passed to [ILanguageModel](ILanguageModel.md).`PromptAsync`.
`null` options means "use the model's configured
defaults verbatim".

#### Parameters

- `TokenLimit` — Maximum response tokens for this call; `null` = use the configured limit.
- `Tools` — Tool groups visible to the model for this call; default = no tools.
- `InjectEphemeralContext` — When `true`, the inference runner renders the per-turn prompt-context template and inserts the resulting ephemeral turn into the prompt before dispatch; defaults to `false` so callers that want raw inference (e.g. compaction) keep their current behavior.
- `EmitTurns` — When `true`, the inference runner publishes the resulting Thought / Assistant `Turn` events after the stream completes; `false` for callers that consume the produced text directly without appending it to a conversation (e.g. compaction).

