namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Convenience extensions over <see cref="IEventBus"/> that
/// generate a fresh UUIDv7 correlation id when the caller is starting
/// a new event chain.
/// </summary>
public static class EventPublisherExtensions
{
    /// <summary>
    /// Publishes <paramref name="data"/> with a freshly generated
    /// correlation id (UUIDv7).
    /// </summary>
    public static async ValueTask PublishAsync<T>(this IEventBus publisher,
        EventType eventType,
        T? data,
        CancellationToken cancellationToken)
        where T : class
    {
        await publisher.PublishAsync(eventType, data, Guid.CreateVersion7(), cancellationToken);
    }

    /// <summary>
    /// Publishes a payload-less event of <paramref name="eventType"/>
    /// with a freshly generated correlation id (UUIDv7).
    /// </summary>
    public static async ValueTask PublishAsync<T>(this IEventBus publisher,
        EventType eventType,
        CancellationToken cancellationToken)
        where T : class
    {
        await publisher.PublishAsync<T>(eventType, null, Guid.CreateVersion7(), cancellationToken);
    }
}
