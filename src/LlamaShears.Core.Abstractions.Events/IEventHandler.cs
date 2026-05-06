namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Handles events for a single payload type. Implement this on a class
/// when an event handler has its own state, dependencies, or lifetime;
/// for one-shot handlers prefer the delegate overload of
/// <see cref="IEventBus.Subscribe{T}"/>.
/// </summary>
/// <typeparam name="T">The payload type this handler observes.</typeparam>
public interface IEventHandler<T>
    where T : class
{
    /// <summary>
    /// Invoked by the bus for each delivered envelope that matches the
    /// subscription's pattern and delivery mode.
    /// </summary>
    ValueTask HandleAsync(IEventEnvelope<T> envelope, CancellationToken cancellationToken);
}
