using System.Reflection;
using System.Runtime.Loader;

namespace StrangeSoft.Plugins.Host;

/// <summary>
/// Resolves an assembly by consulting the deps.json that ships next
/// to a component (typically a plugin's main DLL or its directory).
/// Wraps <see cref="AssemblyDependencyResolver"/>; when the resolver
/// produces a path, the assembly is loaded into the requesting
/// <see cref="AssemblyLoadContext"/> via
/// <see cref="AssemblyLoadContext.LoadFromAssemblyPath(string)"/>.
/// </summary>
public sealed class PathAssemblyResolver : IAssemblyResolver
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// Builds a resolver rooted at <paramref name="componentPath"/> —
    /// either the component assembly's file path or the directory
    /// containing it. <see cref="AssemblyDependencyResolver"/> reads
    /// the adjacent deps.json to know what file paths to hand back
    /// for which assembly identities.
    /// </summary>
    public PathAssemblyResolver(string componentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentPath);
        _resolver = new AssemblyDependencyResolver(componentPath);
    }

    /// <inheritdoc />
    public Assembly? Resolve(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path is null) return null;
        return context.LoadFromAssemblyPath(path);
    }
}
