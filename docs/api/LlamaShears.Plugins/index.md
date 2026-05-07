# LlamaShears.Plugins

The `IPlugin` contract a [LlamaShears](https://github.com/jasoncouture/llama-shears) plugin author compiles against. Pair it with [`StrangeSoft.Plugins.Abstractions`](https://www.nuget.org/packages/StrangeSoft.Plugins.Abstractions) (for the generic `IPluginLoader<T>` chassis entry point) to build a loadable plugin.

## Public surface

- **`IPlugin`** — the LlamaShears plugin lifecycle:
  - `Register(IServiceCollection)` — add services. Runs inside a transactional snapshot so a throwing `Register` rolls the container back.
  - `Build(IApplicationBuilder)` — opportunity to wire pipeline steps after the host app is built. Default no-op.
  - `InitializeAsync(IServiceProvider, CancellationToken)` — async startup hook. Default no-op.
  - `UnloadingAsync(CancellationToken)` — async teardown hook. Default no-op.
  - `Name` — defaults to the implementation type name.

## What you also need

- **`StrangeSoft.Plugins.Abstractions`** — provides `IPluginLoader<IPlugin>`. Your plugin assembly exposes a class implementing this; the host's loader chassis calls it to materialize your `IPlugin` instances.

A reference plugin lives at [`samples/HelloWorld.LlamaShears.Plugin`](https://github.com/jasoncouture/llama-shears/tree/main/samples/HelloWorld.LlamaShears.Plugin) — the minimum viable shape (one `IPlugin` plus one `IPluginLoader<IPlugin>`).

## See also

- [Architecture overview — Plugin SDK section](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/architecture.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.

---

## LlamaShears.Plugins

- [IPlugin](LlamaShears/Plugins/IPlugin.md)

