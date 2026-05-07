# StrangeSoft.Plugins.Host.IPluginContext<T>

Assembly: `StrangeSoft.Plugins.Host`

Materialized plugin-loading scope: a per-plugin
AssemblyLoadContext already
populated with the plugin's root assembly, plus the machinery to
discover and instantiate every `IPluginLoader<T>` it
contributes.

## Type Parameters

- `T` — The host's plugin contract. The discovery walk looks for types
implementing `StrangeSoft.Plugins.Abstractions.IPluginLoader<T>`
for this exact T.

## Properties

### `AssemblyLoadContext`

The underlying AssemblyLoadContext
the plugin is loaded into. Hosts that need lower-level access
(e.g., to subscribe to the context's own events, walk loaded
assemblies, or trigger collection on a collectible context) can
reach the ALC directly through this property.

## Methods

### `AddAssemblyResolver`([IAssemblyResolver](IAssemblyResolver.md) resolver)

Appends an additional [IAssemblyResolver](IAssemblyResolver.md) to the
chain consulted when the runtime fails to resolve an assembly
referenced by code in this context. Resolvers are walked in
the order they were added; the first non-null result wins.

### `LoadFromAssemblyName`(AssemblyName assemblyName)

Loads an assembly into this context by identity. Equivalent to
AssemblyLoadContext.`LoadFromAssemblyName`
on [IPluginContext](IPluginContext-1.md).`AssemblyLoadContext`.

### `LoadFromAssemblyPath`(string assemblyPath)

Loads an assembly into this context from a file path. Equivalent
to AssemblyLoadContext.`LoadFromAssemblyPath`
on [IPluginContext](IPluginContext-1.md).`AssemblyLoadContext`.

### `LoadPluginsAsync`(CancellationToken cancellationToken)

Discovers loader types within the context's loaded assemblies,
invokes each loader concurrently, and streams the resulting
plugins as they arrive. Loader exceptions are swallowed and
surface as an empty contribution from that loader.

## Events

### `Resolving`

Forwarded `Resolving` event from the underlying
[IPluginContext](IPluginContext-1.md).`AssemblyLoadContext`. Subscribe here to react when
resolution falls through every [IAssemblyResolver](IAssemblyResolver.md)
added to the chain. Returning a non-null assembly satisfies
the binding; returning `null` lets resolution
continue to fail.

