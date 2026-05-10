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

### `DeleteContext`(string key)

Forcibly removes the scope keyed by `key`.

### `StartContextAsync`(string key, IEnumerable<[IDataContextItemProvider](IDataContextItemProvider.md)> providers, CancellationToken cancellationToken)

Creates a new scope keyed by `key`, populates it
from `providers`, and sets it as the active scope
on the current call chain. Throws when a live scope already
claims that key.

### `TryJoinContextScope`(string key, [IDataContextScope](IDataContextScope.md)& context)

Joins an existing scope identified by `key` as
the current call chain's active scope. Throws if the call chain
already has a different scope. Returns `false`
when no scope with that key is alive.

