# StrangeSoft.Plugins.IPluginContext<T>

Assembly: `StrangeSoft.Plugins`

Materialized plugin-loading scope: a per-plugin
AssemblyLoadContext already
populated with the plugin's root assembly, plus the machinery to
discover and instantiate every `IPluginLoader<T>` it
contributes.

## Type Parameters

- `T` — The host's plugin contract. The discovery walk looks for types
implementing `StrangeSoft.Plugins.Abstractions.IPluginLoader<T>`
for this exact T.

## Methods

### `LoadPluginsAsync`(CancellationToken cancellationToken)

Discovers loader types within the context's loaded assemblies,
invokes each loader concurrently, and streams the resulting
plugins as they arrive. Loader exceptions are swallowed and
surface as an empty contribution from that loader.

