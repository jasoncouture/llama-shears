namespace LlamaShears.Agent.Abstractions;

/// <summary>
/// A clock-driven "frame" tick published on the internal MessagePipe
/// bus at a fixed cadence. Anything that wants to do work on a regular
/// short interval — schedulers, agent heartbeat dispatchers,
/// housekeeping, etc. — subscribes to this message. The frame tick
/// is intentionally not a heartbeat: it is the lower-level clock
/// signal that a separate heartbeat dispatcher (per-agent, longer
/// cadence) consumes to decide which agents to fire.
/// </summary>
/// <param name="At">UTC instant the tick was emitted by the publisher.</param>
public record FrameTick(DateTimeOffset At);
