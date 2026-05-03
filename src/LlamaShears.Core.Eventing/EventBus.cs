using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Events;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Eventing;

internal sealed class EventBus : IEventBus, IEventPublisher
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
        _logger.LogInformation("Publishing fire and forget event: {Envelope}", envelope);
        publisher.Publish(envelope, cancellationToken);
        envelope = envelope with { DeliveryMode = EventDeliveryMode.Awaited };
        _logger.LogInformation("Publishing awaited event: {Envelope}", envelope);
        await publisher.PublishAsync(envelope, cancellationToken);
        _logger.LogTrace("Event publishing complete");
    }

    public IDisposable Subscribe<T>(string? pattern, EventDeliveryMode mode, IEventHandler<T> handler) where T : class
    {
        using var loggerScope = _logger.BeginScope("{EventTypePattern} {EventDataType} {EventMode} {EventHandlerType}", pattern, typeof(T), mode, handler.GetType());
        _logger.LogDebug("Adding subscription with pattern {EventTypePattern}", pattern);
        var handlerWrapper = ActivatorUtilities.CreateInstance<EventHandlerWrapper<T>>(_serviceProvider, handler, new EventHandlerWrapperOptions(pattern, mode));
        var asyncSubscriber = _serviceProvider.GetRequiredService<IAsyncSubscriber<IEventEnvelope<T>>>();
        var subscription = asyncSubscriber.Subscribe(handlerWrapper);
        _logger.LogInformation("Subscribed {EventHandlerType} to {EventType} with filter pattern {EventTypePattern}", handler.GetType(), typeof(T), pattern);
        return subscription;
    }

    sealed record EventHandlerWrapperOptions(string? Pattern, EventDeliveryMode DeliveryMode);

    sealed class EventHandlerWrapper<T> : IAsyncMessageHandler<IEventEnvelope<T>> where T : class
    {
        private readonly IEventHandler<T> _handler;
        private readonly string? _pattern;
        private readonly EventDeliveryMode _deliveryMode;
        private readonly IPatternMatcher _patternMatcher;
        

        public EventHandlerWrapper(IEventHandler<T> handler, EventHandlerWrapperOptions options, IPatternMatcher patternMatcher)
        {
            _handler = handler;
            _pattern = options.Pattern;
            _deliveryMode = options.DeliveryMode;
            _patternMatcher = patternMatcher;
        }

        public async ValueTask HandleAsync(IEventEnvelope<T> envelope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(envelope.DeliveryMode != _deliveryMode) return;
            if(!string.IsNullOrWhiteSpace(_pattern) && !_patternMatcher.IsMatch(_pattern, envelope.Type)) return;
            await _handler.HandleAsync(envelope, cancellationToken);
        }
    }
    record EventEnvelope<T>(EventType Type, EventDeliveryMode DeliveryMode, Guid CorrelationId, T? Data) : IEventEnvelope<T> where T : class;
}
