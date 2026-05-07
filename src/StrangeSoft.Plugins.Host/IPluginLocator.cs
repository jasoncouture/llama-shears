using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

namespace StrangeSoft.Plugins.Host;

public interface IPluginLocator<T> where T : class
{
    IEnumerable<PluginInformation> GetPluginInformation();
}
