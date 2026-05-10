# LlamaShears.Core.Abstractions.Agent.AgentEmbeddingConfig

Assembly: `LlamaShears.Core.Abstractions`

Per-agent embedding-model selection used for memory search.
Asymmetric prefixes are supplied here so the framework, not the
caller, knows whether to decorate "this is a query" vs "this is a
document being indexed".

## Parameters

- `Id` — Provider/model identifier of the embedding model.
- `KeepAlive` — Provider-specific keep-alive for the model; `null` uses provider default.
- `QueryPrefix` — Prefix prepended to texts being embedded as a query (asymmetric models only).
- `DocumentPrefix` — Prefix prepended to texts being embedded as a document (asymmetric models only).
- `Options` — Free-form provider/model JSON overrides merged on top of host defaults.

## Properties

### `DocumentPrefix`

Prefix prepended to texts being embedded as a document (asymmetric models only).

### `Id`

Provider/model identifier of the embedding model.

### `KeepAlive`

Provider-specific keep-alive for the model; `null` uses provider default.

### `Options`

Free-form provider/model JSON overrides merged on top of host defaults.

### `QueryPrefix`

Prefix prepended to texts being embedded as a query (asymmetric models only).

## Methods

### `AgentEmbeddingConfig`([CompositeIdentity](../Provider/CompositeIdentity.md) Id, Nullable<TimeSpan> KeepAlive, string QueryPrefix, string DocumentPrefix, Nullable<JsonElement> Options)

Per-agent embedding-model selection used for memory search.
Asymmetric prefixes are supplied here so the framework, not the
caller, knows whether to decorate "this is a query" vs "this is a
document being indexed".

#### Parameters

- `Id` — Provider/model identifier of the embedding model.
- `KeepAlive` — Provider-specific keep-alive for the model; `null` uses provider default.
- `QueryPrefix` — Prefix prepended to texts being embedded as a query (asymmetric models only).
- `DocumentPrefix` — Prefix prepended to texts being embedded as a document (asymmetric models only).
- `Options` — Free-form provider/model JSON overrides merged on top of host defaults.

