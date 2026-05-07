# StrangeSoft.Plugins.Host

## Types

- [HostContextAssemblyResolver](HostContextAssemblyResolver.md) — Resolves any assembly the host already owns by binding it to AssemblyLoadContext.`Default`'s loaded copy. The host-owned name set is populated once at startup by [HostContextAssemblyResolver](HostContextAssemblyResolver.md).`Initialize`, which walks the host's transitive references and eagerly loads them into Default. Later resolution calls just check membership and ask Default to satisfy the binding — guaranteeing type identity unifies between the host and any plugin that touches a host-owned type.
- [IAssemblyResolver](IAssemblyResolver.md) — Hook supplied to an AssemblyLoadContext's `Resolving` event. Implementations decide where (or whether) to load an assembly the runtime couldn't otherwise find.
- [IPluginContextLogger](IPluginContextLogger.md) — Notification sink for events that happen inside the plugin loader — host graph walk, plugin loader instantiation, plugin loader invocation. The loader prefers to swallow per-item failures so a single bad assembly or loader doesn't take everything down; this interface gives the host a place to surface those failures (or not) in whatever logging stack it owns.
- [IPluginContext<T>](IPluginContext-1.md) — Materialized plugin-loading scope: a per-plugin AssemblyLoadContext already populated with the plugin's root assembly, plus the machinery to discover and instantiate every `IPluginLoader<T>` it contributes.
- [PathAssemblyResolver](PathAssemblyResolver.md) — Resolves an assembly by consulting the deps.json that ships next to a component (typically a plugin's main DLL or its directory). Wraps AssemblyDependencyResolver; when the resolver produces a path, the assembly is loaded into the requesting AssemblyLoadContext via AssemblyLoadContext.`LoadFromAssemblyPath`.
- [PluginContext<T>](PluginContext-1.md)

