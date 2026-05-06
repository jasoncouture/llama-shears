# LlamaShears.Core.Plugin.Core

## Types

- [IPlugin](IPlugin.md) — Contract every plugin exposes. Instances are produced by the assembly's [IPluginLoader](IPluginLoader.md) and threaded through the host's startup, request pipeline, and lifecycle.
- [IPluginLoader](IPluginLoader.md) — Assembly-level entry point for plugin discovery. A plugin assembly declares one type implementing this contract; the host's loader instantiates it (parameterless ctor) and calls [IPluginLoader](IPluginLoader.md).`LoadAsync` to obtain the plugins the assembly elects to activate.

