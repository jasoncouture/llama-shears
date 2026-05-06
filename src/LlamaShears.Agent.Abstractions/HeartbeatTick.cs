namespace LlamaShears.Agent.Abstractions;

/// <summary>
/// Periodic tick message published on the internal MessagePipe bus.
/// Anything that wants to do work on a regular cadence (agents,
/// schedulers, housekeeping, etc.) subscribes to this message.
/// </summary>
/// <param name="At">UTC instant the tick was emitted by the publisher.</param>
public record HeartbeatTick(DateTimeOffset At);
