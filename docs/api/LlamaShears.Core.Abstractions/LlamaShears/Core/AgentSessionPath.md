# LlamaShears.Core.AgentSessionPath

Assembly: `LlamaShears.Core.Abstractions`

Materialised parent-chain of session ids for an agent, ordered root-last. Built lazily by
the repository when full ancestry is needed for logging or routing.

## Parameters

- `Segments` — Session ids in current-to-root order.

## Properties

### `Depth`

Number of ancestor hops from this session to the root.

### `Segments`

Session ids in current-to-root order.

## Methods

### `AgentSessionPath`(ImmutableArray<Guid> Segments)

Materialised parent-chain of session ids for an agent, ordered root-last. Built lazily by
the repository when full ancestry is needed for logging or routing.

#### Parameters

- `Segments` — Session ids in current-to-root order.

### `ToString`

