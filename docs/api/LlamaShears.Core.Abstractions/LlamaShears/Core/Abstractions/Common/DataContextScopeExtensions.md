# LlamaShears.Core.Abstractions.Common.DataContextScopeExtensions

Assembly: `LlamaShears.Core.Abstractions`

Convenience accessors over [IDataContextScope](IDataContextScope.md).

## Methods

### `Snapshot`([IDataContextScope](IDataContextScope.md) scope)

Returns an immutable snapshot of `scope`'s current
dictionary. Throws when the receiver is `null`; sites
that snapshot the scope legitimately cannot proceed without one.
Subsequent mutations to the scope do not affect the returned value.

