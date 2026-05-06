# LlamaShears.Core.Abstractions.Provider.IEmbeddingModel

Assembly: `LlamaShears.Core.Abstractions.Provider`

Provider-agnostic seam for generating embeddings. Parallel to
[ILanguageModel](ILanguageModel.md); an underlying provider may implement
chat, embeddings, or both. Implementations send the supplied text
to their underlying API verbatim — any model-specific decoration
(asymmetric query/document prefixes, normalization) is the caller's
responsibility, configured separately.

## Methods

### `EmbedAsync`(IReadOnlyList<string> texts, CancellationToken cancellationToken)

Batched form. The result has the same length and order as
`texts`. An empty input yields an empty result.

### `EmbedAsync`(string texts, CancellationToken cancellationToken)

Embeds `text` and returns its vector.
Dimensionality is determined by the model and is reflected in
the length of the returned memory.

