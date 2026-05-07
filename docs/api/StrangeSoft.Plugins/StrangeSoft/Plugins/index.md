# StrangeSoft.Plugins

## Types

- [IAssemblyResolver](IAssemblyResolver.md) — Hook supplied to an AssemblyLoadContext's `Resolving` event. Implementations decide where (or whether) to load an assembly the runtime couldn't otherwise find.
- [IPluginContext<T>](IPluginContext-1.md) — Materialized plugin-loading scope: a per-plugin AssemblyLoadContext already populated with the plugin's root assembly, plus the machinery to discover and instantiate every `IPluginLoader<T>` it contributes.

