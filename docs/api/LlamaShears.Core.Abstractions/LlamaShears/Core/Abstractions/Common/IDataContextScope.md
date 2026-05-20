# LlamaShears.Core.Abstractions.Common.IDataContextScope

Assembly: `LlamaShears.Core.Abstractions`

A keyed bag of arbitrary values flowing on the current call chain.
Backed by a stack-of-dictionaries so callers can [IDataContextScope](IDataContextScope.md).`BeginScope`
to overlay temporary mutations without leaking them out of the using
block.

## Properties

### `Key`

The session this scope belongs to, assigned at start.

## Methods

### `BeginScope`

Pushes the current dictionary onto a stack and creates a copy as
the new working set. Disposing the returned token pops back to
the snapshot, discarding any mutations made while in scope.

### `Clear`

Clears every entry in the current dictionary.

### `Remove`(string key)

Removes the entry at `key` from the current
dictionary. Returns `true` when an entry was
removed, `false` when no entry existed at that
key.

### `SetItem`(string key, object value)

Sets `key` to the provided value.
Existing keys are overwritten.

### `SetItems`(IEnumerable<KeyValuePair<string, object>> items)

Sets each `items` pair into the current
dictionary. Existing keys are overwritten.

### `SetItemsAsync`([IDataContextItemProvider](IDataContextItemProvider.md) provider, CancellationToken cancellationToken)

Materializes `provider`'s items into the current
dictionary. Existing keys are overwritten.

