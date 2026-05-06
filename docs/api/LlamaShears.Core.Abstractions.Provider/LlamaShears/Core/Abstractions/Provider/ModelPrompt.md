# LlamaShears.Core.Abstractions.Provider.ModelPrompt

Assembly: `LlamaShears.Core.Abstractions.Provider`

Provider-agnostic prompt: an ordered list of [ModelTurn](ModelTurn.md)
values destined for an [ILanguageModel](ILanguageModel.md). Providers
translate it into their wire format; consumers do not see that
translation.

## Parameters

- `Turns` — Turns making up the prompt, in chronological order.

## Properties

### `Turns`

Turns making up the prompt, in chronological order.

## Methods

### `ModelPrompt`(IReadOnlyList<[ModelTurn](ModelTurn.md)> Turns)

Provider-agnostic prompt: an ordered list of [ModelTurn](ModelTurn.md)
values destined for an [ILanguageModel](ILanguageModel.md). Providers
translate it into their wire format; consumers do not see that
translation.

#### Parameters

- `Turns` — Turns making up the prompt, in chronological order.

