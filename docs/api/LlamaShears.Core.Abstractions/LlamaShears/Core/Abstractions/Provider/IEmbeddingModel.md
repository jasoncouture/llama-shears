# LlamaShears.Core.Abstractions.Provider.IEmbeddingModel

Assembly: `LlamaShears.Core.Abstractions`

Provider-agnostic seam for generating embeddings. Parallel to
[ILanguageModel](ILanguageModel.md); an underlying provider may implement
chat, embeddings, or both. Implementations send the supplied text
to their underlying API verbatim — any model-specific decoration
(asymmetric query/document prefixes, normalization) is the caller's
responsibility, configured separately.

## Methods

### `EmbedAsync`(string text, CancellationToken cancellationToken)

Embeds `text` and returns its vector.
Dimensionality is determined by the model and is reflected in
the length of the returned memory.

