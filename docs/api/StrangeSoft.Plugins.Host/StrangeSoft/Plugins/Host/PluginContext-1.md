# StrangeSoft.Plugins.Host.PluginContext<T>

Assembly: `StrangeSoft.Plugins.Host`

## Methods

### `CreateDefaultPluginContext`(string rootAssemblyFile, string name, [IPluginContextLogger](IPluginContextLogger.md) logger)

Convenience over [PluginContext](PluginContext-1.md).`CreatePluginContext` that wires the
canonical resolver chain: [HostContextAssemblyResolver](HostContextAssemblyResolver.md).`Instance`
first so host-owned names bind to Default, then a per-plugin
[PathAssemblyResolver](PathAssemblyResolver.md) for the plugin's deps.json.

### `CreatePluginContext`(string rootAssemblyFile, string name, [IPluginContextLogger](IPluginContextLogger.md) logger)

Bare construction: spins up an [PluginContext](PluginContext-1.md).`AssemblyLoadContext`
for `rootAssemblyFile`, loads the root assembly,
and returns the context with no resolvers attached. Use this when
the host wants full control over the resolver chain.

