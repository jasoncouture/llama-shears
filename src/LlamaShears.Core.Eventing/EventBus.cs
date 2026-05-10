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
        LogSubscribed(_logger, handler.GetType(), typeof(T), pattern);
        return new SubscriptionHandle(subscription, _logger, handler.GetType(), typeof(T), pattern);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Subscribed {EventHandlerType} to {EventType} with filter pattern {EventTypePattern}")]
    private static partial void LogSubscribed(ILogger logger, Type eventHandlerType, Type eventType, string? eventTypePattern);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Unsubscribed {EventHandlerType} from {EventType} with filter pattern {EventTypePattern}")]
    private static partial void LogUnsubscribed(ILogger logger, Type eventHandlerType, Type eventType, string? eventTypePattern);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Handler {EventHandlerType} for {EventType} ({DeliveryMode}) observed cancellation; stopping.")]
    private static partial void LogHandlerCancelled(ILogger logger, Type eventHandlerType, Type eventType, EventDeliveryMode deliveryMode);

    private sealed class SubscriptionHandle : IDisposable
    {
        private readonly IDisposable _inner;
        private readonly ILogger _logger;
        private readonly Type _handlerType;
        private readonly Type _eventType;
        private readonly string? _pattern;
        private int _disposed;

        public SubscriptionHandle(IDisposable inner, ILogger logger, Type handlerType, Type eventType, string? pattern)
        {
            _inner = inner;
            _logger = logger;
            _handlerType = handlerType;
            _eventType = eventType;
            _pattern = pattern;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _inner.Dispose();
            LogUnsubscribed(_logger, _handlerType, _eventType, _pattern);
        }
    }

    sealed record EventHandlerWrapperOptions(string? Pattern, EventDeliveryMode DeliveryMode);

    sealed class EventHandlerWrapper<T> : IAsyncMessageHandler<IEventEnvelope<T>> where T : class
    {
        private readonly IEventHandler<T> _handler;
        private readonly string? _pattern;
        private readonly EventDeliveryMode _deliveryMode;
        private readonly IPatternMatcher _patternMatcher;
        private readonly ILogger<EventBus> _logger;

        public EventHandlerWrapper(IEventHandler<T> handler, EventHandlerWrapperOptions options, IPatternMatcher patternMatcher, ILogger<EventBus> logger)
        {
            _handler = handler;
            _pattern = options.Pattern;
            _deliveryMode = options.DeliveryMode;
            _patternMatcher = patternMatcher;
            _logger = logger;
        }

        public async ValueTask HandleAsync(IEventEnvelope<T> envelope, CancellationToken cancellationToken)
        {
            if (envelope.DeliveryMode != _deliveryMode) return;
            if (!string.IsNullOrWhiteSpace(_pattern) && !_patternMatcher.IsMatch(_pattern, envelope.Type)) return;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _handler.HandleAsync(envelope, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogHandlerCancelled(_logger, _handler.GetType(), typeof(T), _deliveryMode);
            }
        }
    }
    record EventEnvelope<T>(EventType Type, EventDeliveryMode DeliveryMode, Guid CorrelationId, T? Data) : IEventEnvelope<T> where T : class;
}
