using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Per-session inbound queue for turns the model still needs to see.
/// Carries two kinds of inputs — user messages arriving from channels,
/// and tool-result turns produced by dispatched tool calls — and
/// returns them to the run loop in the order strict providers require:
/// any pending tool turns first, followed by an optional same-channel
/// user batch.
/// </summary>
public interface ISessionQueue
{
    /// <summary>
    /// Appends <paramref name="turn"/> to the appropriate internal lane.
    /// User turns batch by <see cref="ModelTurn.ChannelId"/>; tool turns
    /// drain ahead of any pending user batch on the next dequeue.
    /// </summary>
    /// <param name="turn">The turn to queue. Must have <c>Role</c> set to <see cref="ModelRole.User"/> or <see cref="ModelRole.Tool"/>.</param>
    /// <param name="cancellationToken">Cancellation for the underlying channel write (typically completes synchronously).</param>
    ValueTask EnqueueAsync(ModelTurn turn, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the next batch the model should process. Drain order:
    /// <list type="number">
    ///   <item>All currently-queued tool turns (non-blocking).</item>
    ///   <item>If any tool turns drained, also drain a same-channel user batch (non-blocking) and append it.</item>
    ///   <item>If no tool turns were available, block until at least one user turn arrives, then drain the same-channel batch.</item>
    /// </list>
    /// The returned array is never empty unless the call was cancelled
    /// or the queue has been completed; callers should treat empty as
    /// "shutting down".
    /// </summary>
    ValueTask<ImmutableArray<ModelTurn>> DequeueBatchAsync(CancellationToken cancellationToken);
}
