# LlamaShears.Plugins.Host

Host-side DI plumbing for the [LlamaShears](https://github.com/jasoncouture/llama-shears) plugin chassis. The host references this; **plugin authors do not** — plugins compile against [`LlamaShears.Plugins`](https://www.nuget.org/packages/LlamaShears.Plugins) and [`StrangeSoft.Plugins.Abstractions`](https://www.nuget.org/packages/StrangeSoft.Plugins.Abstractions).

## Public surface

- **`PluginServiceCollectionExtensions`** —
  - `LoadPluginsAsync(this IServiceCollection, failureCallback, ct, params IPluginLocator<IPlugin>[])` — drive the full discovery → load → register pipeline from the host's composition root.
  - `UsePluginsAsync(this IApplicationBuilder, ct)` — sweep the loaded `IPlugin` singletons through `InitializeAsync` and `Build` once the app is built.
  - `TryApplyPluginsAsync(this IServiceCollection, IPluginContext<IPlugin>, …)` — single-context apply path for callers that already own plugin contexts.
  - `AddPluginDefferedLogger(this IServiceCollection, Assembly?)` — wires `DefaultPluginContextLogger.Instance` to a `DeferredPluginHostLogger` that buffers calls until DI is built.
  - `Snapshot(this IServiceCollection)` — transactional snapshot used internally by `TryApplyPlugin`; safe to use directly when an external caller needs the same atomic-register behaviour.
- **`IServiceCollectionSnapshot`** / **`ServiceCollectionSnapshot`** — capture-and-restore semantics for `IServiceCollection` so a partially-applied plugin can roll back cleanly.
- **`DeferredPluginHostLogger`** / **`DeferredLogEntry`** — implementations of the [`StrangeSoft.Plugins.Host`](https://www.nuget.org/packages/StrangeSoft.Plugins.Host) `IPluginContextLogger` that record log calls in arrival order under a monitor lock; `RedirectTo(ILogger)` flushes them onto the real logger once DI is up.

## See also

- [Architecture overview — Plugin SDK section](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/architecture.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.

---

## LlamaShears.Plugins.Host

- [DeferredLogEntry](LlamaShears/Plugins/Host/DeferredLogEntry.md)
- [DeferredPluginHostLogger](LlamaShears/Plugins/Host/DeferredPluginHostLogger.md)
- [IServiceCollectionSnapshot](LlamaShears/Plugins/Host/IServiceCollectionSnapshot.md)

