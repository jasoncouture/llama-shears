# LlamaShears.Core.Abstractions.Provider.PromptOptions

Assembly: `LlamaShears.Core.Abstractions.Provider`

Per-call overrides passed to [ILanguageModel](ILanguageModel.md).`PromptAsync`.
`null` options means "use the model's configured
defaults verbatim".

## Parameters

- `TokenLimit` — Maximum response tokens for this call; `null` = use the configured limit.
- `Tools` — Tool groups visible to the model for this call; default = no tools.

## Properties

### `TokenLimit`

Maximum response tokens for this call; `null` = use the configured limit.

### `Tools`

Tool groups visible to the model for this call; default = no tools.

## Methods

### `PromptOptions`(Nullable<int> TokenLimit, ImmutableArray<ToolGroup> Tools)

Per-call overrides passed to [ILanguageModel](ILanguageModel.md).`PromptAsync`.
`null` options means "use the model's configured
defaults verbatim".

#### Parameters

- `TokenLimit` — Maximum response tokens for this call; `null` = use the configured limit.
- `Tools` — Tool groups visible to the model for this call; default = no tools.

