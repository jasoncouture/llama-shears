namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Publishes events to the in-process bus. Implementations are expected to
/// fan out each call to both fire-and-forget and awaited delivery so
/// subscribers can opt into either mode.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event of the given <paramref name="eventType"/> carrying
    /// <paramref name="data"/>.
    /// </summary>
    ValueTask PublishAsync<T>(EventType eventType, T? data, Guid? correlationId = null)
        where T : class;
}

