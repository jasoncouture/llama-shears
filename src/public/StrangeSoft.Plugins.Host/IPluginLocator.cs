namespace StrangeSoft.Plugins.Host;

public interface IPluginLocator<T> where T : class
{
    IEnumerable<PluginInformation> GetPluginInformation();
}
