# StrangeSoft.Plugins.Abstractions

The plugin-author entry-point contract for the StrangeSoft generic plugin chassis. A plugin assembly exposes one or more `IPluginLoader<T>` implementations; the host's loader machinery (in [`StrangeSoft.Plugins.Host`](https://www.nuget.org/packages/StrangeSoft.Plugins.Host)) instantiates each via parameterless constructor and calls `LoadAsync` to materialize the plugin instances.

The chassis is intentionally generic — `T` is the host's plugin contract, defined by the host. [LlamaShears](https://github.com/jasoncouture/llama-shears) uses `IPluginLoader<IPlugin>` (where `IPlugin` is from `LlamaShears.Plugins`); other hosts could use the same chassis with their own contract.

## Public surface

- **`IPluginLoader<T>`**
  - `Task<ImmutableArray<T>> LoadAsync(CancellationToken)` — return the plugins this assembly contributes. An empty array is the canonical "this assembly chose not to activate" signal (feature-flag off, host runtime mismatch, …); the host treats it as a successful no-op rather than an error.

## See also

- [`StrangeSoft.Plugins.Host`](https://www.nuget.org/packages/StrangeSoft.Plugins.Host) — the host-side chassis.
- [`LlamaShears.Plugins`](https://www.nuget.org/packages/LlamaShears.Plugins) — the LlamaShears `IPlugin` contract.
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.

---

## StrangeSoft.Plugins.Abstractions

- [IPluginLoader<T>](StrangeSoft/Plugins/Abstractions/IPluginLoader-1.md)

