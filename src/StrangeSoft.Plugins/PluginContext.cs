using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using StrangeSoft.Plugins.Abstractions;

namespace StrangeSoft.Plugins;

public class PluginContext<T> : IPluginContext<T> where T : class
{
    private readonly IAssemblyResolver _resolver;
    private readonly AssemblyLoadContext _context;

    private PluginContext(IAssemblyResolver resolver, AssemblyLoadContext context)
    {
        _resolver = resolver;
        _context = context;
    }

    public static IPluginContext<T>? CreatePluginContext(string rootAssemblyFile, string name, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootAssemblyFile);
        rootAssemblyFile = Path.GetFullPath(rootAssemblyFile);
        if (!File.Exists(rootAssemblyFile)) throw new ArgumentException("Root assembly file must exist!");
        var directory = Path.GetDirectoryName(rootAssemblyFile);
        if (directory is null) throw new DirectoryNotFoundException("Could not get plugin directory from plugin path");
        var resolver = PluginAssemblyResolver.GetOrCreate(directory);
        var context = new AssemblyLoadContext(name);
        context.Resolving += resolver.Resolve;
        context.LoadFromAssemblyPath(rootAssemblyFile);

        return new PluginContext<T>(resolver, context);
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
        catch
        {
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
            catch
            {
                continue;
            }
            if (result is IPluginLoader<T> loader) yield return loader;
        }
    }
}
