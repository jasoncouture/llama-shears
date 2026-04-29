using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Abstractions;

/// <summary>
/// An agent: an autonomous component that periodically heartbeats and
/// communicates via input and output channels rather than returning
/// data to a caller. Inputs are accepted from any of
/// <see cref="InputChannels"/> and outputs are sent to all of
/// <see cref="OutputChannels"/>; both append to <see cref="Context"/>.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Wall-clock time the agent's last heartbeat completed.
    /// </summary>
    DateTimeOffset LastHeartbeatAt { get; }

    /// <summary>
    /// Desired cadence between heartbeats. The heartbeat dispatcher
    /// derives heartbeat firings from the host's <see cref="SystemTick"/>
    /// (currently every 30 seconds), so periods shorter than the frame
    /// interval effectively fire every frame.
    /// </summary>
    TimeSpan HeartbeatPeriod { get; }

    /// <summary>
    /// Whether heartbeats are currently enabled for this agent. The
    /// heartbeat dispatcher skips agents whose value is
    /// <see langword="false"/>.
    /// </summary>
    bool HeartbeatEnabled { get; }

    /// <summary>
    /// The agent's current conversational context. Grows as turns arrive
    /// from <see cref="InputChannels"/> and as the agent emits turns to
    /// <see cref="OutputChannels"/>.
    /// </summary>
    IReadOnlyList<ModelTurn> Context { get; }

    /// <summary>
    /// Sources the agent reads input turns from. Inputs from any channel
    /// are accepted and appended to <see cref="Context"/>.
    /// </summary>
    IReadOnlyList<IInputChannel> InputChannels { get; }

    /// <summary>
    /// Destinations the agent sends produced turns to. Outputs are
    /// fanned out to every channel in this list and appended to
    /// <see cref="Context"/>.
    /// </summary>
    IReadOnlyList<IOutputChannel> OutputChannels { get; }
}
