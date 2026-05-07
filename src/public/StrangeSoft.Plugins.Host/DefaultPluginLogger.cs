namespace StrangeSoft.Plugins.Host;

public sealed class DefaultPluginContextLogger
{
    public static IPluginContextLogger Instance { get; set; } = NullPluginContextLogger.Instance;
}
