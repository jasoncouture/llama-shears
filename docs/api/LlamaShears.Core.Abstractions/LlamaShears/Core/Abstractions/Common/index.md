# LlamaShears.Core.Abstractions.Common

## Types

- [AsyncDataContextServiceScope](AsyncDataContextServiceScope.md) — Decorator that owns an AsyncServiceScope alongside an IDisposable data-context frame. Disposing the decorator tears down the data frame and the DI scope as one operation.
- [CompositeIdentity](CompositeIdentity.md) — Two-part identifier that pairs a provider name with a provider-scoped model id to form a globally unique handle. Serializes to/from `"<provider>/<model>"` for human readability and concise JSON.
- [CompositeIdentityJsonConverter](CompositeIdentityJsonConverter.md) — JSON converter for [CompositeIdentity](CompositeIdentity.md): serializes as the compact string `"provider/model"` rather than as an object.
- [CompositeIdentityTypeConverter](CompositeIdentityTypeConverter.md) — TypeConverter for [CompositeIdentity](CompositeIdentity.md) so it flows through configuration binding and similar string-pivoted machinery as `"provider/model"`.
- [DataContextConstants](DataContextConstants.md) — Constants used by the data-context infrastructure.
- [DataContextScopeExtensions](DataContextScopeExtensions.md) — Convenience accessors over [IDataContextScope](IDataContextScope.md).
- [DataContextScopeFactoryExtensions](DataContextScopeFactoryExtensions.md) — Composition helpers that pair a DI IServiceScope with a new [IDataContextScope](IDataContextScope.md).`BeginScope` frame so the two share a single disposable lifetime.
- [DataContextServiceCollectionExtensions](DataContextServiceCollectionExtensions.md) — DI registration helpers for [IDataContextItemProvider](IDataContextItemProvider.md).
- [HostData](HostData.md) — Host-level system info exposed on the data context under the `host` key. Captured once at process start; all consumers (templates, services, prompts) see the same snapshot.
- [IDataContextFactory](IDataContextFactory.md) — Manages keyed [IDataContextScope](IDataContextScope.md) instances flowing on the current call chain via AsyncLocal. Other call chains looking up the same key can rejoin via [IDataContextFactory](IDataContextFactory.md).`TryJoinContextScope`.
- [IDataContextItemProvider](IDataContextItemProvider.md) — Contributes key/value pairs into the current data-context scope. Implementations should not throw; on failure, return an empty enumerable. The factory aggregates items from every registered provider when a context starts.
- [IDataContextScope](IDataContextScope.md) — A keyed bag of arbitrary values flowing on the current call chain. Backed by a stack-of-dictionaries so callers can [IDataContextScope](IDataContextScope.md).`BeginScope` to overlay temporary mutations without leaking them out of the using block.
- [IPersistentDataContextItem](IPersistentDataContextItem.md) — Marker interface. Values implementing this survive a [IDataContextScope](IDataContextScope.md).`BeginScope` pop: when the inner scope is disposed, any key whose current value implements this marker is copied into the parent dictionary before the parent is restored as the working set.

