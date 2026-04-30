namespace LlamaShears.Hosting;

/// <summary>
/// One-shot work that runs once during host startup. Implementations
/// are resolved from a fresh DI scope created by the runner when the
/// host starts, executed in registration order, and then discarded. A
/// throwing task aborts host startup.
/// </summary>
public interface IHostStartupTask
{
    /// <summary>
    /// Executes the startup work. May use scoped dependencies resolved
    /// alongside this instance. Honour
    /// <paramref name="cancellationToken"/> — host shutdown during
    /// startup signals it.
    /// </summary>
    ValueTask StartAsync(CancellationToken cancellationToken);
}
