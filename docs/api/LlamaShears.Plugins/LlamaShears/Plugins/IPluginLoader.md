# LlamaShears.Plugins.IPluginLoader

Assembly: `LlamaShears.Plugins`

Assembly-level entry point for plugin discovery. A plugin assembly
declares one type implementing this contract; the host's loader
instantiates it (parameterless ctor) and calls [IPluginLoader](IPluginLoader.md).`LoadAsync`
to obtain the plugins the assembly elects to activate.

## Methods

### `LoadAsync`(CancellationToken cancellationToken)

Returns the plugins this assembly contributes. An empty array
is the canonical "this assembly chose not to activate" signal
(feature flag off, host runtime mismatch, etc.); the host
treats it as a successful no-op rather than an error.

