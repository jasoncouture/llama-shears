# StrangeSoft.Plugins.Host

Generic host-side plugin chassis for the StrangeSoft plugin SDK. Reusable outside [LlamaShears](https://github.com/jasoncouture/llama-shears) — the `T` in `IPluginContext<T>` and friends is whatever plugin contract a host wants its plugins to implement.

## What this gives you

- A per-plugin `AssemblyLoadContext` that isolates plugin code while still letting host-shared types (the host's `IPlugin` interface, `ILogger<>`, framework primitives) flow through with **stable type identity** — anything in the host's transitive reference graph binds to the Default ALC's copy, so a plugin's `IPlugin` is the same `Type` as the host's `IPlugin`.
- A locator surface for "where do I find plugins?" decoupled from the chassis: `IPluginLocator<T>` produces `PluginInformation` records (path, name, resolver-chain choice, optional extra resolvers); the chassis turns each into an `IPluginContext<T>`.
- An orchestrator (`Plugin.LoadPluginContexts`) that walks every locator, builds contexts, and yields them lazily so failures on one plugin don't abort the rest.

## Public surface

### Chassis

- **`PluginContext<T>`** / **`IPluginContext<T>`** — the per-plugin scope (its `AssemblyLoadContext`, resolver chain, plugin-discovery `LoadPluginsAsync` async iterator).
- **`Plugin`** static — `LoadPluginContexts<T>(...)` orchestrator, plus the `CreatePluginContext` / `CreateDefaultPluginContext` factory facades.
- **`IPluginLocator<T>`** — host-side discovery interface.
- **`PluginInformation`** — record describing one discovered plugin (name, path, default-resolver toggle, additional resolvers).

### Resolvers

- **`IAssemblyResolver`** — chained resolver hook for `AssemblyLoadContext.Resolving`.
- **`HostContextAssemblyResolver`** — singleton; eagerly walks the host's transitive ref graph at `Initialize` time, then short-circuits any host-owned name to the Default ALC's copy.
- **`PathAssemblyResolver`** — wraps `AssemblyDependencyResolver` for the per-plugin dep set.

### Logging

- **`IPluginContextLogger`** — ILogger-style sink (`Debug`/`Information`/`Warning`/`Error`) the chassis calls during load. Implementation's responsibility to wire to its own logging stack.
- **`NullPluginContextLogger`** — silent default.
- **`DefaultPluginContextLogger`** — global singleton getter so chassis call sites can log without injection plumbing.

## See also

- [`StrangeSoft.Plugins.Abstractions`](https://www.nuget.org/packages/StrangeSoft.Plugins.Abstractions) — the plugin-author surface (`IPluginLoader<T>`).
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears) — reference host that uses this chassis.

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.
