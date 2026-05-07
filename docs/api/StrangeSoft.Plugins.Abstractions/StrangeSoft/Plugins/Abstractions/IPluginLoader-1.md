# StrangeSoft.Plugins.Abstractions.IPluginLoader<T>

Assembly: `StrangeSoft.Plugins.Abstractions`

Assembly-level entry point a plugin assembly exposes for a host
expecting plugins of type `T`. The host loader
instantiates this via parameterless constructor and calls
[IPluginLoader](IPluginLoader-1.md).`LoadAsync` to materialize the plugin instances.

## Type Parameters

- `T` — The host's plugin contract. The generic SDK has no opinion on its
shape — the host defines it (interface, base class, anything).

## Methods

### `LoadAsync`(CancellationToken cancellationToken)

Returns the plugins this assembly contributes. An empty array
is the canonical "this assembly chose not to activate" signal
(feature flag off, host runtime mismatch, etc.); the host
treats it as a successful no-op rather than an error.

