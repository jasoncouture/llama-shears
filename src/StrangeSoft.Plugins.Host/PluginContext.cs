using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using StrangeSoft.Plugins.Abstractions;

namespace StrangeSoft.Plugins.Host;

public class PluginContext<T> : IPluginContext<T> where T : class
{
    private readonly AssemblyLoadContext _context;
    private readonly IPluginContextLogger _logger;
    private readonly List<IAssemblyResolver> _userResolvers = [];
    private readonly IAssemblyResolver _pathResolver;

    private PluginContext(AssemblyLoadContext context, IPluginContextLogger logger, IAssemblyResolver pathResolver)
    {
        _context = context;
        _logger = logger;
        _pathResolver = pathResolver;
        _context.Resolving += OnResolving;
    }

    public AssemblyLoadContext AssemblyLoadContext => _context;

    public void AddAssemblyResolver(IAssemblyResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _userResolvers.Add(resolver);
    }

    public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
        => _context.LoadFromAssemblyName(assemblyName);

    public Assembly LoadFromAssemblyPath(string assemblyPath)
        => _context.LoadFromAssemblyPath(assemblyPath);

    private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        // User-supplied resolvers run first, in registration order.
        foreach (var resolver in _userResolvers)
        {
            var assembly = resolver.Resolve(context, assemblyName);
            if (assembly is not null) return assembly;
        }

        // Built-in fallback chain — host wins for host-owned names so type identity
        // unifies with the host; otherwise consult the plugin's own deps.json.
        var hostHit = HostContextAssemblyResolver.Instance.Resolve(context, assemblyName);
        if (hostHit is not null) return hostHit;
        return _pathResolver.Resolve(context, assemblyName);
    }

    public static IPluginContext<T>? CreatePluginContext(string rootAssemblyFile, string name, IPluginContextLogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootAssemblyFile);
        rootAssemblyFile = Path.GetFullPath(rootAssemblyFile);
        if (!File.Exists(rootAssemblyFile)) throw new ArgumentException("Root assembly file must exist!");
        var directory = Path.GetDirectoryName(rootAssemblyFile);
        if (directory is null) throw new DirectoryNotFoundException("Could not get plugin directory from plugin path");
        var pathResolver = new PathAssemblyResolver(directory);
        var context = new AssemblyLoadContext(name);
        var pluginContext = new PluginContext<T>(context, logger ?? NullPluginContextLogger.Instance, pathResolver);
        context.LoadFromAssemblyPath(rootAssemblyFile);

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
            return await loader.LoadAsync(cancellationToken).WaitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LoaderInvocationFailed(loader.GetType(), ex);
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
                var constructor = loaderType.GetConstructor([]);
                if (constructor is null) continue;
                result = constructor.Invoke(null, []);
            }
            catch (Exception ex)
            {
                _logger.LoaderInstantiationFailed(loaderType, ex);
                continue;
            }
            if (result is IPluginLoader<T> loader) yield return loader;
        }
    }
}
