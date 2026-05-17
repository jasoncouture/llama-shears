namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// A service whose lifetime is tied to a single agent: it runs on
/// agent startup and stays alive for the life of that agent. Useful
/// for event handlers, background tasks, or anything that needs the
/// agent's scope to be live when it begins work.
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Called once when the owning agent's run loop is starting up.
    /// Implementations should return quickly; long-running work
    /// belongs on a background task that the implementation owns.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Called once when the owning agent is shutting down. The agent's
    /// scope is still live; implementations should finish any pending
    /// work and release resources before returning.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
}
