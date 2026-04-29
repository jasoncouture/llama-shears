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
}
