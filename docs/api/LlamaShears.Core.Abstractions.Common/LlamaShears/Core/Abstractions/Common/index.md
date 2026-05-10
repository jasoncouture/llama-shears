# LlamaShears.Core.Abstractions.Common

## Types

- [DataContextConstants](DataContextConstants.md) — Constants used by the data-context infrastructure.
- [DataContextServiceCollectionExtensions](DataContextServiceCollectionExtensions.md) — DI registration helpers for [IDataContextItemProvider](IDataContextItemProvider.md).
- [IDataContextFactory](IDataContextFactory.md) — Manages keyed [IDataContextScope](IDataContextScope.md) instances flowing on the current call chain via AsyncLocal. Other call chains looking up the same key can rejoin via [IDataContextFactory](IDataContextFactory.md).`TryJoinContextScope`.
- [IDataContextItemProvider](IDataContextItemProvider.md) — Contributes key/value pairs into the current data-context scope. Implementations should not throw; on failure, return an empty enumerable. The factory aggregates items from every registered provider when a context starts.
- [IDataContextScope](IDataContextScope.md) — A keyed bag of arbitrary values flowing on the current call chain. Backed by a stack-of-dictionaries so callers can [IDataContextScope](IDataContextScope.md).`BeginScope` to overlay temporary mutations without leaking them out of the using block.
- [IPersistentDataContextItem](IPersistentDataContextItem.md) — Marker interface. Values implementing this survive a [IDataContextScope](IDataContextScope.md).`BeginScope` pop: when the inner scope is disposed, any key whose current value implements this marker is copied into the parent dictionary before the parent is restored as the working set.

