# LlamaShears.Core.Abstractions.Provider.IModelTextResponse

Assembly: `LlamaShears.Core.Abstractions`

Streaming fragment carrying user-visible text. Aggregating every
fragment's [IModelTextResponse](IModelTextResponse.md).`Content` in arrival order yields the model's
final response.

## Properties

### `Content`

Textual content delivered in this fragment. The provider is
responsible for terminating the fragment stream when the
response is complete; a "done" flag is unnecessary and absent
by design.

