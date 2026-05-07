using System.Reflection;

namespace StrangeSoft.Plugins.Host;

internal sealed class NullPluginContextLogger : IPluginContextLogger
{
    public static IPluginContextLogger Instance { get; } = new NullPluginContextLogger();

    private NullPluginContextLogger() { }

    public void AssemblyLoadFailed(AssemblyName assemblyName, Exception exception) { }

    public void LoaderInstantiationFailed(Type loaderType, Exception exception) { }

    public void LoaderInvocationFailed(Type loaderType, Exception exception) { }
}
