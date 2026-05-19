namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Convenience extensions over <see cref="IEventPublisher"/> that
/// generate a fresh UUIDv7 correlation id when the caller is starting
/// a new event chain.
/// </summary>
public static class EventPublisherExtensions
{
    extension(IEventPublisher publisher)
    {
        /// <summary>
        /// Publishes <paramref name="data"/> with a freshly generated
        /// correlation id (UUIDv7).
        /// </summary>
        public async ValueTask PublishAsync<T>(EventType eventType,
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
        public async ValueTask PublishAsync<T>(EventType eventType,
            CancellationToken cancellationToken)
            where T : class
        {
            await publisher.PublishAsync<T>(eventType, null, Guid.CreateVersion7(), cancellationToken);
        }
    }
}
