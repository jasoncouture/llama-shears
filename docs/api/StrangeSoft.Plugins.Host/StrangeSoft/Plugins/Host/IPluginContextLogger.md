# StrangeSoft.Plugins.Host.IPluginContextLogger

Assembly: `StrangeSoft.Plugins.Host`

Notification sink for events that happen inside the plugin loader
— host graph walk, plugin loader instantiation, plugin loader
invocation. The loader prefers to swallow per-item failures so a
single bad assembly or loader doesn't take everything down; this
interface gives the host a place to surface those failures (or
not) in whatever logging stack it owns.

## Methods

### `AssemblyLoadFailed`(AssemblyName assemblyName, Exception exception)

Reported when Assembly.`Load`
throws during the host's transitive-reference walk. The walk
continues; `assemblyName` simply won't be in
the host-owned set.

### `LoaderInstantiationFailed`(Type loaderType, Exception exception)

Reported when an `IPluginLoader<T>` implementation
can't be constructed — missing parameterless constructor, ctor
throws, etc. The discovery walk skips it and continues with
the next type.

### `LoaderInvocationFailed`(Type loaderType, Exception exception)

Reported when an `IPluginLoader<T>.LoadAsync` call
throws. The loader's contribution is treated as empty; other
loaders' results are still streamed out.

