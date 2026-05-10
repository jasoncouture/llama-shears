# LlamaShears.Core.Abstractions.Common.DataContextConstants

Assembly: `LlamaShears.Core.Abstractions`

Constants used by the data-context infrastructure.

## Fields

### `SingletonKey`

DI key under which singleton [IDataContextItemProvider](IDataContextItemProvider.md)
implementations are registered. The factory consumes this key via
`[FromKeyedServices]` at construction.

