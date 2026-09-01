namespace LlamaShears.Core.Abstractions.Agent.Pipeline;

/// <summary>
/// Scoped slot for the cancellation source of the in-flight turn.
/// Interrupt-scope middleware registers the linked source for the
/// batch; inbound interrupt handling calls <see cref="Cancel"/> so
/// the two do not share a private field on the loop owner.
/// </summary>
public interface IActiveTurnCancellation
{
    /// <summary>
    /// Installs <paramref name="cancellationTokenSource"/> as the
    /// current turn's source. Replaces any previously registered
    /// source without cancelling it.
    /// </summary>
    /// <param name="cancellationTokenSource">The linked source for this batch. Must not be <see langword="null"/>.</param>
    void Register(CancellationTokenSource cancellationTokenSource);

    /// <summary>
    /// Clears the slot when it still holds <paramref name="cancellationTokenSource"/>.
    /// A later batch's source is left alone.
    /// </summary>
    /// <param name="cancellationTokenSource">The source this caller registered.</param>
    void Unregister(CancellationTokenSource cancellationTokenSource);

    /// <summary>
    /// Cancels the registered source, if any. No-op when no turn is
    /// in flight. Does not throw if the source is already cancelled.
    /// </summary>
    void Cancel();
}
