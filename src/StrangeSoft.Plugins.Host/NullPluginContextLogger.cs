namespace StrangeSoft.Plugins.Host;

internal sealed class NullPluginContextLogger : IPluginContextLogger
{
    public static IPluginContextLogger Instance { get; } = new NullPluginContextLogger();

    private NullPluginContextLogger() { }

    public void Debug(string format, params IEnumerable<object?> data) { }
    public void Information(string format, params IEnumerable<object?> data) { }
    public void Warning(string format, Exception? exception, params IEnumerable<object?> data) { }
    public void Error(string format, Exception? exception, params IEnumerable<object?> data) { }
}
