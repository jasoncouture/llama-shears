# LlamaShears.Core.Abstractions.Common.IPersistentDataContextItem

Assembly: `LlamaShears.Core.Abstractions.Common`

Marker interface. Values implementing this survive a
[IDataContextScope](IDataContextScope.md).`BeginScope` pop: when the inner scope
is disposed, any key whose current value implements this marker is
copied into the parent dictionary before the parent is restored as
the working set.

