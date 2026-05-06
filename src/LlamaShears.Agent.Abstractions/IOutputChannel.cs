using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Abstractions;

/// <summary>
/// A conceptual output channel for an agent — a destination that
/// receives turns produced by the agent. This is the agent's I/O
/// abstraction; it is unrelated to
/// <see cref="System.Threading.Channels.Channel{T}"/>.
/// </summary>
public interface IOutputChannel
{
    /// <summary>
    /// Sends a turn produced by the agent to this output destination.
    /// </summary>
    Task SendAsync(ModelTurn turn, CancellationToken cancellationToken);
}
