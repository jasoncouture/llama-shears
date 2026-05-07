namespace LlamaShears.Hosting;

/// <summary>
/// Host-wide self-restart trigger. Implementations begin a graceful
/// shutdown immediately and arrange for the process either to be
/// re-spawned (the entrypoint executable is invoked again with the
/// original argv) or to exit with a non-zero status so a container
/// supervisor restarts it.
/// </summary>
public interface IHostRestarter
{
    /// <summary>
    /// Requests the host to restart. Idempotent — repeated calls after
    /// the first are no-ops. Returns synchronously; the actual restart
    /// happens after the application's hosted services have stopped.
    /// </summary>
    void RequestRestart();
}
