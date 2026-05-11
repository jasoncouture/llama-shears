using LlamaShears.Core.Abstractions.Events;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Eventing;

internal sealed partial class EventBus : IEventBus, IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    public async ValueTask PublishAsync<T>(EventType eventType, T? data, Guid correlationId, CancellationToken cancellationToken) where T : class
    {
        using var loggerScope = _logger.BeginScope("{EventType} {EventCorrelationId}", eventType, correlationId);
        var publisher = _serviceProvider.GetRequiredService<IAsyncPublisher<IEventEnvelope<T>>>();
        var envelope = new EventEnvelope<T>(eventType, EventDeliveryMode.FireAndForget, correlationId, data);

        var denied = EventDeliveryMask.None;
        foreach (var filter in _serviceProvider.GetServices<IEventFilter>())
        {
            denied |= await filter.GetDeniedModesAsync(envelope, cancellationToken);
            if (denied == EventDeliveryMask.Both) break;
        }

        if (!denied.HasFlag(EventDeliveryMask.FireAndForget))
        {
            _logger.LogTrace("Publishing fire and forget event: {Envelope}", envelope);
            publisher.Publish(envelope, cancellationToken);
        }

        envelope = envelope with { DeliveryMode = EventDeliveryMode.Awaited };
        if (!denied.HasFlag(EventDeliveryMask.Awaited))
        {
            _logger.LogTrace("Publishing awaited event: {Envelope}", envelope);
            await publisher.PublishAsync(envelope, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogTrace("Event publishing complete");
    }

    public IDisposable Subscribe<T>(string? pattern, EventDeliveryMode mode, IEventHandler<T> handler) where T : class
    {
        using var loggerScope = _logger.BeginScope("{EventTypePattern} {EventDataType} {EventMode} {EventHandlerType}", pattern, typeof(T), mode, handler.GetType());
        _logger.LogDebug("Adding subscription with pattern {EventTypePattern}", pattern);
        var handlerWrapper = ActivatorUtilities.CreateInstance<EventHandlerWrapper<T>>(_serviceProvider, handler, new EventHandlerWrapperOptions(pattern, mode));
        var asyncSubscriber = _serviceProvider.GetRequiredService<IAsyncSubscriber<IEventEnvelope<T>>>();
        var subscription = asyncSubscriber.Subscribe(handlerWrapper);
        LogSubscribed(handler.GetType(), typeof(T), pattern);
        return new SubscriptionHandle(subscription, _logger, handler.GetType(), typeof(T), pattern);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Subscribed {EventHandlerType} to {EventType} with filter pattern {EventTypePattern}")]
    private partial void LogSubscribed(Type eventHandlerType, Type eventType, string? eventTypePattern);

    record EventEnvelope<T>(EventType Type, EventDeliveryMode DeliveryMode, Guid CorrelationId, T? Data) : IEventEnvelope<T> where T : class;
}
