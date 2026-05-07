using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

namespace StrangeSoft.Plugins.Host;

internal sealed class PluginAssemblyResolver : IAssemblyResolver
{
    private static ImmutableHashSet<string>? _hostAssemblyNames;
    private static readonly ConcurrentDictionary<string, IAssemblyResolver> _resolvers = new();

    private readonly AssemblyDependencyResolver _pluginDependencyResolver;

    private PluginAssemblyResolver(AssemblyDependencyResolver dependencyResolver)
    {
        if (_hostAssemblyNames is null) throw new InvalidOperationException("Plugin assembly resolver is not initialized");
        _pluginDependencyResolver = dependencyResolver;
    }

    public Assembly? Resolve(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName.Name)) throw new ArgumentException("Assembly name cannot be null or empty");
        if (_hostAssemblyNames!.Contains(assemblyName.Name)) return null;
        var path = _pluginDependencyResolver.ResolveAssemblyToPath(assemblyName);
        if (path is null) return null;
        return context.LoadFromAssemblyPath(path);
    }

    public static void Initialize(Assembly? hostAssembly = null, IPluginContextLogger? logger = null)
    {
        if (_hostAssemblyNames is not null) throw new InvalidOperationException("Plugin assembly resolver is already initialized and cannot be initialized again.");
        hostAssembly ??= Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        ArgumentNullException.ThrowIfNull(hostAssembly);
        logger ??= NullPluginContextLogger.Instance;
        var hostAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var assemblies = new Stack<Assembly>();
        assemblies.Push(hostAssembly);

        while (assemblies.Count > 0)
        {
            var assembly = assemblies.Pop();
            var needsScan = hostAssemblyNames.Add(assembly.GetName().Name ?? throw new InvalidOperationException("Assembly name cannot be null"));
            if (!needsScan) continue;

            foreach (var assemblyName in assembly.GetReferencedAssemblies())
            {
                if (!TryLoadAssembly(assemblyName, logger, out var currentAssembly)) continue;
                assemblies.Push(currentAssembly);
            }
        }

        _hostAssemblyNames = hostAssemblyNames.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryLoadAssembly(AssemblyName name, IPluginContextLogger logger, [NotNullWhen(true)] out Assembly? assembly)
    {
        assembly = null;
        try
        {
            assembly = Assembly.Load(name);
            return true;
        }
        catch (Exception ex)
        {
            logger.AssemblyLoadFailed(name, ex);
            return false;
        }
    }

    public static IAssemblyResolver GetOrCreate(string pluginBasePath)
    {
        return _resolvers.GetOrAdd(pluginBasePath, CreateAssemblyResolver);
    }

    private static IAssemblyResolver CreateAssemblyResolver(string pluginBasePath)
    {
        return new PluginAssemblyResolver(new AssemblyDependencyResolver(pluginBasePath));
    }
}
