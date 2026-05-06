# LlamaShears.Core.Abstractions.Caching.CacheResult<T>

Assembly: `LlamaShears.Core.Abstractions.Caching`

Outcome of [IShearsCache](IShearsCache-1.md).`TryGet``1`: distinguishes
"no entry", "entry exists under a different type", and "entry hit"
so callers can branch without exception handling.

## Type Parameters

- `TItem` — Type the caller asked the cache to interpret the entry as.

## Parameters

- `Present` — Whether an entry was found and matched `TItem`.
- `TypeMismatch` — Whether an entry existed at the key but its cached value was a different type than `TItem`.
- `Value` — The cached value when `Present` is `true`; otherwise `default`.

## Properties

### `Present`

Whether an entry was found and matched `TItem`.

### `TypeMismatch`

Whether an entry existed at the key but its cached value was a different type than `TItem`.

### `Value`

The cached value when `Present` is `true`; otherwise `default`.

## Methods

### `CacheResult<T>`(bool Present, bool TypeMismatch, _0 Value)

Outcome of [IShearsCache](IShearsCache-1.md).`TryGet``1`: distinguishes
"no entry", "entry exists under a different type", and "entry hit"
so callers can branch without exception handling.

#### Type Parameters

- `TItem` — Type the caller asked the cache to interpret the entry as.

#### Parameters

- `Present` — Whether an entry was found and matched `TItem`.
- `TypeMismatch` — Whether an entry existed at the key but its cached value was a different type than `TItem`.
- `Value` — The cached value when `Present` is `true`; otherwise `default`.

