# LlamaShears.Core.Abstractions.Provider.IContextEntry

Assembly: `LlamaShears.Core.Abstractions`

Base contract for any entry that can be appended to an agent's
conversation log. Polymorphic JSON serialization is keyed by the
`kind` discriminator on the wire.

## Properties

### `Version`

Schema version for the concrete entry shape. Implementations bump
this when the entry's serialized form changes incompatibly so
readers can detect and migrate older payloads.

