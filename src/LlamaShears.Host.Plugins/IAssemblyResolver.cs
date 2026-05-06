using System.Reflection;
using System.Runtime.Loader;

namespace LlamaShears.Host.Plugins;

/// <summary>
/// Hook supplied to an <see cref="AssemblyLoadContext"/>'s <c>Resolving</c>
/// event. Implementations decide where (or whether) to load an assembly
/// the runtime couldn't otherwise find.
/// </summary>
public interface IAssemblyResolver
{
    /// <summary>
    /// Returns the assembly to satisfy <paramref name="assembly"/> in the
    /// scope of <paramref name="context"/>, or <see langword="null"/> to
    /// decline (the runtime continues with normal binding failure).
    /// </summary>
    public Assembly? Resolve(AssemblyLoadContext context, AssemblyName assembly);
}
