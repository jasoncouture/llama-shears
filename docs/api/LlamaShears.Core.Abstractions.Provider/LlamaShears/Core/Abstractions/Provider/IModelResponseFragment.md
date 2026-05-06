# LlamaShears.Core.Abstractions.Provider.IModelResponseFragment

Assembly: `LlamaShears.Core.Abstractions.Provider`

Marker contract for one piece of an [ILanguageModel](ILanguageModel.md)'s
streaming response. Concrete fragment shapes — visible text, hidden
reasoning — implement the more specific
[IModelTextResponse](IModelTextResponse.md) or [IModelThoughtResponse](IModelThoughtResponse.md).

