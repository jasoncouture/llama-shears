# LlamaShears.Core.Abstractions.Provider.ModelConfiguration

Assembly: `LlamaShears.Core.Abstractions`

Construction-time inputs for [IProviderFactory](IProviderFactory.md).`CreateModel`
and [IEmbeddingProviderFactory](IEmbeddingProviderFactory.md).`CreateModel`. Doubles as the
authored agent-config shape — the same record persists to disk and flows
to providers verbatim.

## Parameters

- `Id` — Globally unique model identifier (provider + provider-scoped model name).
- `Think` — Thinking effort hint (chat models only).
- `ContextLength` — Override for the model's context-window size; `null` uses provider default.
- `TokenLimit` — Maximum response tokens; `0` = unbounded.
- `Parameters` — Free-form provider-specific overrides. Captures every JSON property that does not match a known field; providers consume entries (e.g. Ollama reads `keepAlive`).

## Fields

### `DataKey`

Key used to stash the active [ModelConfiguration](ModelConfiguration.md) in the per-turn data context scope.

## Properties

### `ContextLength`

Override for the model's context-window size; `null` uses provider default.

### `Id`

Globally unique model identifier (provider + provider-scoped model name).

### `Parameters`

Free-form provider-specific overrides. Captures every JSON property that does not match a known field; providers consume entries (e.g. Ollama reads `keepAlive`).

### `Think`

Thinking effort hint (chat models only).

### `TokenLimit`

Maximum response tokens; `0` = unbounded.

## Methods

### `ModelConfiguration`([CompositeIdentity](../Common/CompositeIdentity.md) Id, [ThinkLevel](ThinkLevel.md) Think, Nullable<int> ContextLength, int TokenLimit, ImmutableDictionary<string, JsonElement> Parameters)

Construction-time inputs for [IProviderFactory](IProviderFactory.md).`CreateModel`
and [IEmbeddingProviderFactory](IEmbeddingProviderFactory.md).`CreateModel`. Doubles as the
authored agent-config shape — the same record persists to disk and flows
to providers verbatim.

#### Parameters

- `Id` — Globally unique model identifier (provider + provider-scoped model name).
- `Think` — Thinking effort hint (chat models only).
- `ContextLength` — Override for the model's context-window size; `null` uses provider default.
- `TokenLimit` — Maximum response tokens; `0` = unbounded.
- `Parameters` — Free-form provider-specific overrides. Captures every JSON property that does not match a known field; providers consume entries (e.g. Ollama reads `keepAlive`).

