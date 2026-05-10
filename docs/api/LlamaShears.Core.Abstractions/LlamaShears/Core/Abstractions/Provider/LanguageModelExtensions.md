# LlamaShears.Core.Abstractions.Provider.LanguageModelExtensions

Assembly: `LlamaShears.Core.Abstractions`

Convenience extensions over [ILanguageModel](ILanguageModel.md).

## Methods

### `PromptAsync`([ILanguageModel](ILanguageModel.md) model, [ModelPrompt](ModelPrompt.md) prompt, CancellationToken cancellationToken)

Calls [ILanguageModel](ILanguageModel.md).`PromptAsync` with no per-call
option overrides — equivalent to passing `null`
for the options argument.

