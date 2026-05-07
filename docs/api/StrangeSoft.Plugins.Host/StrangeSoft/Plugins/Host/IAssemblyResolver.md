# StrangeSoft.Plugins.Host.IAssemblyResolver

Assembly: `StrangeSoft.Plugins.Host`

Hook supplied to an AssemblyLoadContext's `Resolving`
event. Implementations decide where (or whether) to load an assembly
the runtime couldn't otherwise find.

## Methods

### `Resolve`(AssemblyLoadContext context, AssemblyName assembly)

Returns the assembly to satisfy `assembly` in the
scope of `context`, or `null` to
decline (the runtime continues with normal binding failure).

