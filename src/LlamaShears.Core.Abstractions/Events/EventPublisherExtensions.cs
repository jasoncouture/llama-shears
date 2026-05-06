namespace LlamaShears.Core.Abstractions.Events;

public static class EventPublisherExtensions
{
    public static async ValueTask PublishAsync<T>(
        this IEventPublisher publisher,
        EventType eventType,
        T? data,
        CancellationToken cancellationToken)
        where T : class
    {
        await publisher.PublishAsync(eventType, data, Guid.CreateVersion7(), cancellationToken);
    }

    public static async ValueTask PublishAsync<T>(
        this IEventPublisher publisher,
        EventType eventType,
        CancellationToken cancellationToken)
        where T : class
    {
        await publisher.PublishAsync<T>(eventType, null, Guid.CreateVersion7(), cancellationToken);
    }
}
