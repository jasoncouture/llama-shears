# LlamaShears.Core.Abstractions.Provider.PromptOptions

Assembly: `LlamaShears.Core.Abstractions`

Per-call overrides passed to [ILanguageModel](ILanguageModel.md).`PromptAsync`.
`null` options means "use the model's configured
defaults verbatim".

## Parameters

- `TokenLimit` — Maximum response tokens for this call; `null` = use the configured limit.
- `Tools` — Tool groups visible to the model for this call; default = no tools.
- `EmitTurns` — When `true`, the inference runner publishes the resulting Thought / Assistant `Turn` events after the stream completes; `false` for callers that consume the produced text directly without appending it to a conversation (e.g. compaction).

## Properties

### `EmitTurns`

When `true`, the inference runner publishes the resulting Thought / Assistant `Turn` events after the stream completes; `false` for callers that consume the produced text directly without appending it to a conversation (e.g. compaction).

### `TokenLimit`

Maximum response tokens for this call; `null` = use the configured limit.

### `Tools`

Tool groups visible to the model for this call; default = no tools.

## Methods

### `PromptOptions`(Nullable<int> TokenLimit, ImmutableArray<[ToolGroup](ToolGroup.md)> Tools, bool EmitTurns)

Per-call overrides passed to [ILanguageModel](ILanguageModel.md).`PromptAsync`.
`null` options means "use the model's configured
defaults verbatim".

#### Parameters

- `TokenLimit` — Maximum response tokens for this call; `null` = use the configured limit.
- `Tools` — Tool groups visible to the model for this call; default = no tools.
- `EmitTurns` — When `true`, the inference runner publishes the resulting Thought / Assistant `Turn` events after the stream completes; `false` for callers that consume the produced text directly without appending it to a conversation (e.g. compaction).

