using System.Collections.Immutable;
using LlamaShears.Plugins;
using StrangeSoft.Plugins.Abstractions;
using StrangeSoft.Plugins.Host;

namespace LlamaShears.PluginLoaders;

public sealed class PathPluginLoader : IPluginLocator<IPlugin>
{
    private readonly ImmutableArray<string> _paths;

    public PathPluginLoader(params IEnumerable<string> paths)
    {
        _paths = [.. FilterPaths(paths)];
    }

    private static IEnumerable<string> FilterPaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            Console.WriteLine("Probing plugin path: {0}", path);
            if (File.Exists(path))
            {
                var fullPath = Path.GetFullPath(path);
                Console.WriteLine("Plugin found at {0}!", fullPath);
                yield return path;
            }
            else
            {
                Console.WriteLine("Could not find plugin at {0}", path);
            }
        }
    }

    public IEnumerable<PluginInformation> GetPluginInformation()
    {
        foreach (var path in _paths)
        {
            var name = Path.GetDirectoryName(path);
            name = Path.GetFileName(name ?? path);
            yield return new PluginInformation(name, path, true);
        }
    }
}