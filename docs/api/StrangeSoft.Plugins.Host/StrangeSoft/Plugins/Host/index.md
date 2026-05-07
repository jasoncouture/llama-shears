# StrangeSoft.Plugins.Host

## Types

- [IAssemblyResolver](IAssemblyResolver.md) — Hook supplied to an AssemblyLoadContext's `Resolving` event. Implementations decide where (or whether) to load an assembly the runtime couldn't otherwise find.
- [IPluginContextLogger](IPluginContextLogger.md) — Notification sink for events that happen inside the plugin loader — host graph walk, plugin loader instantiation, plugin loader invocation. The loader prefers to swallow per-item failures so a single bad assembly or loader doesn't take everything down; this interface gives the host a place to surface those failures (or not) in whatever logging stack it owns.
- [IPluginContext<T>](IPluginContext-1.md) — Materialized plugin-loading scope: a per-plugin AssemblyLoadContext already populated with the plugin's root assembly, plus the machinery to discover and instantiate every `IPluginLoader<T>` it contributes.

