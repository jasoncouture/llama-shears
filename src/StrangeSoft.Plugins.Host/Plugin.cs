using System.Reflection;

namespace StrangeSoft.Plugins.Host;

public static class Plugin
{
    public static void SetDefaultLogger(IPluginContextLogger logger)
    {
        DefaultPluginContextLogger.Instance = logger;
    }
    public static IEnumerable<IPluginContext<T>> LoadPluginContexts<T>(Assembly hostAssembly, params IEnumerable<IPluginLocator<T>> locators) where T : class
    {
        HostContextAssemblyResolver.TryInitialize(hostAssembly);
        foreach (var (locator, pluginInformation) in locators.SelectMany(i => i.GetPluginInformation().Select(x => (i, x))))
        {
            IPluginContext<T>? loaded;
            try
            {
                loaded = LoadPluginContext<T>(pluginInformation);
            }
            catch (Exception ex)
            {
                DefaultPluginContextLogger.Instance.Warning("Failed to load plugin {Path} provided by {Locator}", ex, pluginInformation.Path, locator.GetType());
                continue;
            }
            if (loaded is null)
            {
                DefaultPluginContextLogger.Instance.Warning("Failed to load plugin {Path} provided by {Locator}, load attempt returned null", null, pluginInformation.Path, locator.GetType());
                continue;
            }
            yield return loaded;
        }
    }

    private static IPluginContext<T> LoadPluginContext<T>(PluginInformation pluginInformation) where T : class
    {
        var context = pluginInformation.UseDefaultResolvers switch
        {
            true => CreateDefaultPluginContext<T>(pluginInformation.Path, pluginInformation.Name),
            false => CreatePluginContext<T>(pluginInformation.Path, pluginInformation.Name)
        };

        foreach (var additionalResolver in pluginInformation.AdditionalResolvers)
        {
            context.AddAssemblyResolver(additionalResolver);
        }

        return context;
    }

    public static IEnumerable<IPluginContext<T>> LoadPluginContexts<T>(params IEnumerable<IPluginLocator<T>> locators) where T : class
    {
        return LoadPluginContexts(null!, locators);
    }

    public static IPluginContext<T> CreatePluginContext<T>(string rootAssemblyFile, string name, IPluginContextLogger? logger = null) where T : class
    {
        return PluginContext<T>.CreatePluginContext(rootAssemblyFile, name, logger);
    }

    public static IPluginContext<T> CreateDefaultPluginContext<T>(string rootAssemblyFile, string name, IPluginContextLogger? logger = null) where T : class
    {
        return PluginContext<T>.CreateDefaultPluginContext(rootAssemblyFile, name, logger);
    }
}
