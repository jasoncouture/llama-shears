namespace LlamaShears.Agent.Abstractions;

/// <summary>
/// Clock-driven tick published on the internal MessagePipe bus at a
/// fixed cadence. Subscribers are anything wanting work on a regular
/// short interval (schedulers, heartbeat dispatchers, housekeeping).
/// The tick is intentionally not a heartbeat: heartbeat dispatchers
/// are layered on top of it.
/// </summary>
public record SystemTick(DateTimeOffset At);
