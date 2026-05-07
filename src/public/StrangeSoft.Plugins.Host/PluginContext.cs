using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using StrangeSoft.Plugins.Abstractions;

namespace StrangeSoft.Plugins.Host;

public class PluginContext<T> : IPluginContext<T> where T : class
{
    private class PluginContextAssemblyLoadLogger : IAssemblyResolver
    {
        private IPluginContextLogger _logger;

        public PluginContextAssemblyLoadLogger(IPluginContextLogger logger)
        {
            _logger = logger;
        }

        public Assembly? Resolve(AssemblyLoadContext context, AssemblyName assembly)
        {
            _logger.Debug("Plugin loading context {ContextName} is Loading {Assembly}", context.Name, assembly);
            return null;
        }
    }
    private readonly AssemblyLoadContext _context;
    private readonly IPluginContextLogger _logger;
    private readonly List<IAssemblyResolver> _resolvers = [];

    private PluginContext(AssemblyLoadContext context, IPluginContextLogger logger)
    {
        _context = context;
        _logger = logger;
        _context.Resolving += OnResolving;
        AddAssemblyResolver(new PluginContextAssemblyLoadLogger(logger));
    }

    public AssemblyLoadContext AssemblyLoadContext => _context;

    public void AddAssemblyResolver(IAssemblyResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolvers.Add(resolver);
    }

    public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
        => _context.LoadFromAssemblyName(assemblyName);

    public Assembly LoadFromAssemblyPath(string assemblyPath)
        => _context.LoadFromAssemblyPath(assemblyPath);

    private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        foreach (var resolver in _resolvers)
        {
            var assembly = resolver.Resolve(context, assemblyName);
            if (assembly is not null) return assembly;
        }
        return null;
    }

    /// <summary>
    /// Bare construction: spins up an <see cref="AssemblyLoadContext"/>
    /// for <paramref name="rootAssemblyFile"/>, loads the root assembly,
    /// and returns the context with no resolvers attached. Use this when
    /// the host wants full control over the resolver chain.
    /// </summary>
    public static IPluginContext<T> CreatePluginContext(string rootAssemblyFile, string name, IPluginContextLogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootAssemblyFile);
        rootAssemblyFile = Path.GetFullPath(rootAssemblyFile);
        if (!File.Exists(rootAssemblyFile)) throw new ArgumentException("Root assembly file must exist!");
        var context = new AssemblyLoadContext(name);
        var pluginContext = new PluginContext<T>(context, logger ?? DefaultPluginContextLogger.Instance);
        context.LoadFromAssemblyPath(rootAssemblyFile);
        return pluginContext;
    }

    /// <summary>
    /// Convenience over <see cref="CreatePluginContext"/> that wires the
    /// canonical resolver chain: <see cref="HostContextAssemblyResolver.Instance"/>
    /// first so host-owned names bind to Default, then a per-plugin
    /// <see cref="PathAssemblyResolver"/> for the plugin's deps.json.
    /// </summary>
    public static IPluginContext<T> CreateDefaultPluginContext(string rootAssemblyFile, string name, IPluginContextLogger? logger = null)
    {
        var pluginContext = CreatePluginContext(rootAssemblyFile, name, logger);
        var directory = Path.GetDirectoryName(Path.GetFullPath(rootAssemblyFile))
            ?? throw new DirectoryNotFoundException("Could not get plugin directory from plugin path");
        pluginContext.AddAssemblyResolver(HostContextAssemblyResolver.Instance);
        pluginContext.AddAssemblyResolver(new PathAssemblyResolver(directory));
        return pluginContext;
    }

    public async IAsyncEnumerable<T> LoadPluginsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tasks = new List<Task<ImmutableArray<T>>>();
        foreach (var loader in _context.Assemblies.SelectMany(i => CollectLoaderTypes(i.GetTypes())))
        {
            // Outer WaitAsync guards against a misbehaving loader that blocks synchronously
            // before its task is ever returned (e.g., long-running setup before the first
            // await). The inner WaitAsync inside LoadFromAsync handles the case where the
            // loader's task is returned but never completes.
            tasks.Add(LoadFromAsync(loader, cancellationToken).WaitAsync(cancellationToken));
        }

        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks).ConfigureAwait(false);
            tasks.Remove(completed);
            if (completed.IsCanceled) continue;
            if (completed.IsFaulted) continue;

            foreach (var plugin in await completed)
            {
                yield return plugin;
            }
        }
    }

    private async Task<ImmutableArray<T>> LoadFromAsync(IPluginLoader<T> loader, CancellationToken cancellationToken)
    {
        // We can't trust that a loader will actually switch to async. Force entry into
        // the async state machine immediately so the returned Task can be awaited /
        // wrapped externally even if the loader's synchronous portion is long-running.
        await Task.Yield();
        try
        {

            var plugins = await loader.LoadAsync(cancellationToken).WaitAsync(cancellationToken);
            if (plugins.Length > 0)
            {
                _logger.Information("Loaded {Count} plugins using {LoaderType}", plugins.Length, loader.GetType());
                foreach (var plugin in plugins)
                {
                    _logger.Information("{LoaderType} loaded {PluginName}", loader.GetType(), plugin.GetType());
                }
            }

            return plugins;
        }
        catch (Exception ex)
        {
            _logger.Error("Plugin loader {LoaderType} threw during LoadAsync", ex, loader.GetType());
            return [];
        }
    }

    private IEnumerable<IPluginLoader<T>> CollectLoaderTypes(Type[] types)
    {
        var maybeEligible = types.Where(i => i.IsAssignableTo(typeof(IPluginLoader<T>)) && i.IsClass && !i.IsAbstract);
        foreach (var loaderType in maybeEligible)
        {
            object? result;
            try
            {
                _logger.Debug("Attempting to create instance of {Type} plugin loader", loaderType);
                result = Activator.CreateInstance(loaderType);
            }
            catch (MissingMethodException)
            {
                _logger.Debug("Unable to find default constructor on {Type}, cannot construct plugin.", loaderType);
                continue;
            }
            catch (TargetInvocationException ex)
            {
                _logger.Warning("Failed to instantiate plugin loader {LoaderType}", ex.InnerException ?? ex, loaderType);
                continue;
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to instantiate plugin loader {LoaderType}", ex, loaderType);
                continue;
            }
            if (result is IPluginLoader<T> loader)
            {
                _logger.Information("Plugin loader {Type} loaded", loaderType);
                yield return loader;
            }
        }
    }
}
