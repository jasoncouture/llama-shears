# LlamaShears.Core.Abstractions.Common.IDataContextFactory

Assembly: `LlamaShears.Core.Abstractions`

Manages keyed [IDataContextScope](IDataContextScope.md) instances flowing on
the current call chain via AsyncLocal.
Other call chains looking up the same key can rejoin via
[IDataContextFactory](IDataContextFactory.md).`TryJoinContextScope`.

## Properties

### `Current`

The scope active on the current call chain, or `null`.

## Methods

### `ClearCurrent`(bool owner)

Detaches the current scope from this call chain. When
`owner` is `true` the underlying
scope is also removed from the factory's registry.

### `CreateContext`(string key)

Creates a new empty scope keyed by `key` and sets it
as the current call chain's active scope. Throws when a live scope
already claims that key. The returned scope must be populated via
[IDataContextFactory](IDataContextFactory.md).`InitializeAsync` before consumers read from it.

### `DeleteContext`(string key)

Forcibly removes the scope keyed by `key`.

### `InitializeAsync`(string key, IEnumerable<[IDataContextItemProvider](IDataContextItemProvider.md)> scopeProviders, IEnumerable<KeyValuePair<string, object>> values, CancellationToken cancellationToken)

Populates the scope keyed by `key`: `values`
are written first (so providers can observe them), singleton item
providers contribute next (only when the scope is otherwise empty),
then call-site scoped providers (`scopeProviders`)
contribute on top.

### `TryJoinContextScope`(string key, [IDataContextScope](IDataContextScope.md)& context)

Joins an existing scope identified by `key` as
the current call chain's active scope. Throws if the call chain
already has a different scope. Returns `false`
when no scope with that key is alive.

