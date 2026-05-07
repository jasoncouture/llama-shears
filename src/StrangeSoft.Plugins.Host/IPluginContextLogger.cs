namespace StrangeSoft.Plugins.Host;

/// <summary>
/// Notification sink for events that happen inside the plugin loader
/// — host graph walk, plugin loader instantiation, plugin loader
/// invocation. The loader prefers to swallow per-item failures so a
/// single bad assembly or loader doesn't take everything down; this
/// interface gives the host a place to surface those failures (or
/// not) in whatever logging stack it owns.
/// </summary>
/// <remarks>
/// Format strings follow the .NET <c>ILogger</c> message-template
/// convention (named placeholders like <c>{AssemblyName}</c>); it is
/// the implementation's responsibility to wire that through to its
/// underlying logging stack.
/// </remarks>
public interface IPluginContextLogger
{
    /// <summary>
    /// Records a diagnostic-level message — verbose detail useful when
    /// inspecting loader behavior, normally off in production.
    /// </summary>
    /// <param name="format">Message template (named placeholders).</param>
    /// <param name="data">Values for the placeholders, in order.</param>
    public void Debug(string format, params IEnumerable<object?> data);

    /// <summary>
    /// Records an informational message — routine progress about the
    /// loader's work that the host may want to surface.
    /// </summary>
    /// <param name="format">Message template (named placeholders).</param>
    /// <param name="data">Values for the placeholders, in order.</param>
    public void Information(string format, params IEnumerable<object?> data);

    /// <summary>
    /// Records a non-fatal problem — the loader recovered (e.g. by
    /// skipping a bad assembly or loader type), but the host may want
    /// to know. Pass the originating exception when one is available.
    /// </summary>
    /// <param name="format">Message template (named placeholders).</param>
    /// <param name="exception">The exception associated with the warning, or <see langword="null"/>.</param>
    /// <param name="data">Values for the placeholders, in order.</param>
    public void Warning(string format, Exception? exception, params IEnumerable<object?> data);

    /// <summary>
    /// Records a failure — something the loader couldn't recover from
    /// at this granularity (e.g. a plugin loader's <c>LoadAsync</c>
    /// threw). Pass the originating exception when one is available.
    /// </summary>
    /// <param name="format">Message template (named placeholders).</param>
    /// <param name="exception">The exception associated with the error, or <see langword="null"/>.</param>
    /// <param name="data">Values for the placeholders, in order.</param>
    public void Error(string format, Exception? exception, params IEnumerable<object?> data);
}
