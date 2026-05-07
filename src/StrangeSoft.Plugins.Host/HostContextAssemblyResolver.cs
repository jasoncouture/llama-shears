using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

namespace StrangeSoft.Plugins.Host;

/// <summary>
/// Resolves any assembly the host already owns by binding it to
/// <see cref="AssemblyLoadContext.Default"/>'s loaded copy. The
/// host-owned name set is populated once at startup by
/// <see cref="Initialize"/>, which walks the host's transitive
/// references and eagerly loads them into Default. Later resolution
/// calls just check membership and ask Default to satisfy the
/// binding — guaranteeing type identity unifies between the host
/// and any plugin that touches a host-owned type.
/// </summary>
public sealed class HostContextAssemblyResolver : IAssemblyResolver
{
    private static ImmutableHashSet<string>? _hostAssemblyNames;

    /// <summary>
    /// Singleton — the resolver carries no per-instance state, only
    /// the global host-owned name set. Add it to a plugin context's
    /// chain (or rely on the built-in fallback that already does so)
    /// when host-shared types must come from Default.
    /// </summary>
    public static HostContextAssemblyResolver Instance { get; } = new();

    private HostContextAssemblyResolver() { }

    /// <inheritdoc />
    public Assembly? Resolve(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName.Name)) throw new ArgumentException("Assembly name cannot be null or empty");
        if (_hostAssemblyNames is null) throw new InvalidOperationException("HostContextAssemblyResolver is not initialized; call HostContextAssemblyResolver.Initialize before any plugin context is created.");
        if (!_hostAssemblyNames.Contains(assemblyName.Name)) return null;
        return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
    }

    public static bool TryInitialize(Assembly? hostAssembly, IPluginContextLogger? logger = null)
    {
        if(_hostAssemblyNames is not null)
        {
            return false;
        }

        Initialize(hostAssembly, logger);
        return true;
    }

    private static Assembly LocateRootAssemblyOrThrow(Assembly? hostAssembly)
    {
        hostAssembly ??= Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        ArgumentNullException.ThrowIfNull(hostAssembly);
        return hostAssembly;
    }

    /// <summary>
    /// Walks <paramref name="hostAssembly"/>'s transitive reference
    /// graph, eagerly loads every reachable assembly into
    /// <see cref="AssemblyLoadContext.Default"/>, and records their
    /// names as the host-owned set this resolver consults. Pass
    /// <see langword="null"/> to default to <see cref="Assembly.GetEntryAssembly"/>.
    /// One-shot — calling twice throws.
    /// </summary>
    public static void Initialize(Assembly? hostAssembly = null, IPluginContextLogger? logger = null)
    {
        if (_hostAssemblyNames is not null) throw new InvalidOperationException("HostContextAssemblyResolver is already initialized and cannot be initialized again.");
        hostAssembly ??= LocateRootAssemblyOrThrow(hostAssembly);
        logger ??= DefaultPluginContextLogger.Instance;
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
            logger.Warning("Failed to load referenced assembly {AssemblyName} during host graph walk", ex, name);
            return false;
        }
    }
}
