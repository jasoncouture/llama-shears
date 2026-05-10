namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Periodic host heartbeat broadcast onto the event bus. Subscribers
/// use it as a coarse "wall-clock advanced" signal — agent idle
/// detection, refreshes, scheduled chores — without each component
/// running its own timer.
/// </summary>
/// <param name="At">Wall-clock time the tick was emitted.</param>
public record SystemTick(DateTimeOffset At);
