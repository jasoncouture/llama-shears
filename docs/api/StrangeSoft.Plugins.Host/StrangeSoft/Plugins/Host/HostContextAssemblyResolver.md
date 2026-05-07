# StrangeSoft.Plugins.Host.HostContextAssemblyResolver

Assembly: `StrangeSoft.Plugins.Host`

Resolves any assembly the host already owns by binding it to
AssemblyLoadContext.`Default`'s loaded copy. The
host-owned name set is populated once at startup by
[HostContextAssemblyResolver](HostContextAssemblyResolver.md).`Initialize`, which walks the host's transitive
references and eagerly loads them into Default. Later resolution
calls just check membership and ask Default to satisfy the
binding — guaranteeing type identity unifies between the host
and any plugin that touches a host-owned type.

## Properties

### `Instance`

Singleton — the resolver carries no per-instance state, only
the global host-owned name set. Add it to a plugin context's
chain (or rely on the built-in fallback that already does so)
when host-shared types must come from Default.

## Methods

### `Initialize`(Assembly hostAssembly, [IPluginContextLogger](IPluginContextLogger.md) logger)

Walks `hostAssembly`'s transitive reference
graph, eagerly loads every reachable assembly into
AssemblyLoadContext.`Default`, and records their
names as the host-owned set this resolver consults. Pass
`null` to default to Assembly.`GetEntryAssembly`.
One-shot — calling twice throws.

### `Resolve`(AssemblyLoadContext context, AssemblyName assemblyName)

