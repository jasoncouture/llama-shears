using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Abstractions;

/// <summary>
/// A conceptual input channel for an agent — a source of conversational
/// turns that the agent should consume and append to its context. This
/// is the agent's I/O abstraction; it is unrelated to
/// <see cref="System.Threading.Channels.Channel{T}"/>.
/// </summary>
public interface IInputChannel
{
    /// <summary>
    /// Reads pending turns from this channel. Implementations may
    /// complete the enumeration after draining, or yield turns as
    /// they arrive.
    /// </summary>
    IAsyncEnumerable<ModelTurn> ReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns a task that completes when the channel has, or believes
    /// it has, pending input ready to be read. Push-capable channels
    /// (e.g. those backed by an external bus) override this to wake an
    /// agent's processing loop on arrival without waiting for a periodic
    /// trigger.
    /// <para>
    /// The default implementation never completes until
    /// <paramref name="cancellationToken"/> is cancelled, so a channel
    /// that opts not to override it has no effect on agent scheduling.
    /// Implementations are not required to be edge-triggered: returning
    /// a completed task while the channel has nothing pending is
    /// permitted (the agent will simply observe an empty
    /// <see cref="ReadAsync"/> result and go back to waiting).
    /// </para>
    /// </summary>
    Task WaitForInputAsync(CancellationToken cancellationToken)
        => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
}
