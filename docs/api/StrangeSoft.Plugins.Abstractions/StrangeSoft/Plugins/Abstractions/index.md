# StrangeSoft.Plugins.Abstractions

## Types

- [IPluginLoader<T>](IPluginLoader-1.md) — Assembly-level entry point a plugin assembly exposes for a host expecting plugins of type `T`. The host loader instantiates this via parameterless constructor and calls [IPluginLoader](IPluginLoader-1.md).`LoadAsync` to materialize the plugin instances.

