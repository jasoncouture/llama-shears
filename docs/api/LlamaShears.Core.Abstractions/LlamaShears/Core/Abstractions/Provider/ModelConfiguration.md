# LlamaShears.Core.Abstractions.Provider.ModelConfiguration

Assembly: `LlamaShears.Core.Abstractions`

Construction-time inputs for [IProviderFactory](IProviderFactory.md).`CreateModel`
and [IEmbeddingProviderFactory](IEmbeddingProviderFactory.md).`CreateModel`.

## Parameters

- `ModelId` — Globally unique model identifier (provider + provider-scoped model name).
- `Think` — Thinking effort hint (chat models only).
- `ContextLength` — Override for the model's context-window size; `null` uses provider default.
- `KeepAlive` — Provider-specific keep-alive; `null` uses provider default.
- `Parameters` — Free-form factory-level parameters.
- `TokenLimit` — Maximum response tokens; `0` = unbounded.
- `AgentOptions` — Agent-supplied JSON options merged on top of the provider's host defaults at request time.

## Fields

### `DataKey`

Key used to stash the active [ModelConfiguration](ModelConfiguration.md) in the per-turn data context scope.

## Properties

### `AgentOptions`

Agent-supplied JSON options merged on top of the provider's host defaults at request time.

### `ContextLength`

Override for the model's context-window size; `null` uses provider default.

### `KeepAlive`

Provider-specific keep-alive; `null` uses provider default.

### `ModelId`

Globally unique model identifier (provider + provider-scoped model name).

### `Parameters`

Free-form factory-level parameters.

### `Think`

Thinking effort hint (chat models only).

### `TokenLimit`

Maximum response tokens; `0` = unbounded.

## Methods

### `ModelConfiguration`([ModelIdentity](ModelIdentity.md) ModelId, [ThinkLevel](ThinkLevel.md) Think, Nullable<int> ContextLength, Nullable<TimeSpan> KeepAlive, IReadOnlyDictionary<string, object> Parameters, int TokenLimit, Nullable<JsonElement> AgentOptions)

Construction-time inputs for [IProviderFactory](IProviderFactory.md).`CreateModel`
and [IEmbeddingProviderFactory](IEmbeddingProviderFactory.md).`CreateModel`.

#### Parameters

- `ModelId` — Globally unique model identifier (provider + provider-scoped model name).
- `Think` — Thinking effort hint (chat models only).
- `ContextLength` — Override for the model's context-window size; `null` uses provider default.
- `KeepAlive` — Provider-specific keep-alive; `null` uses provider default.
- `Parameters` — Free-form factory-level parameters.
- `TokenLimit` — Maximum response tokens; `0` = unbounded.
- `AgentOptions` — Agent-supplied JSON options merged on top of the provider's host defaults at request time.

