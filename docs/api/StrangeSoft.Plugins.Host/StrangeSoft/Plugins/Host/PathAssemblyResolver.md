# StrangeSoft.Plugins.Host.PathAssemblyResolver

Assembly: `StrangeSoft.Plugins.Host`

Resolves an assembly by consulting the deps.json that ships next
to a component (typically a plugin's main DLL or its directory).
Wraps AssemblyDependencyResolver; when the resolver
produces a path, the assembly is loaded into the requesting
AssemblyLoadContext via
AssemblyLoadContext.`LoadFromAssemblyPath`.

## Methods

### `PathAssemblyResolver`(string componentPath)

Builds a resolver rooted at `componentPath` —
either the component assembly's file path or the directory
containing it. AssemblyDependencyResolver reads
the adjacent deps.json to know what file paths to hand back
for which assembly identities.

### `Resolve`(AssemblyLoadContext context, AssemblyName assemblyName)

