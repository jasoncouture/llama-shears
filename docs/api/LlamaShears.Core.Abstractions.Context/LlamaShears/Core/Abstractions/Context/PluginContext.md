# LlamaShears.Core.Abstractions.Context.PluginContext

Assembly: `LlamaShears.Core.Abstractions.Context`

Free-form key/value bag plugins use to surface state on an
[AgentContext](AgentContext.md) snapshot. Keys are namespaced by the
owning plugin to avoid collisions; the framework treats values as
opaque.

## Parameters

- `Data` — The plugin keyspace for this snapshot.

## Properties

### `Data`

The plugin keyspace for this snapshot.

## Methods

### `PluginContext`(ImmutableDictionary<string, object> Data)

Free-form key/value bag plugins use to surface state on an
[AgentContext](AgentContext.md) snapshot. Keys are namespaced by the
owning plugin to avoid collisions; the framework treats values as
opaque.

#### Parameters

- `Data` — The plugin keyspace for this snapshot.

