using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using LlamaShears.Core.Abstractions.Events;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Eventing;

internal sealed partial class EventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private const string EventPublishLogCategory = "Events";
    internal static ActivitySource ActivitySource { get; } = new ActivitySource($"LlamaShears.Core.Eventing.{nameof(EventBus)}", typeof(EventBus).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
    private static Meter _meter {get;} = new Meter($"LlamaShears.Core.Eventing.{nameof(EventBus)}", typeof(EventBus).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    public EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger, ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }
    public async ValueTask PublishAsync<T>(EventType eventType, T? data, Guid correlationId, CancellationToken cancellationToken) where T : class
    {
        using var loggerScope = _logger.BeginScope("{EventType}", eventType);
        using var activity = ActivitySource.StartActivity($"event.publish {eventType.Component}:{eventType.EventName}", ActivityKind.Producer);
        activity?.SetTag("event.id", eventType.ToString());
        activity?.SetTag("event.payload.type", typeof(T).FullName);
        
        try
        {

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
                activity?.SetTag("event.publish.mode", EventDeliveryMask.FireAndForget.ToString("G"));
                _logger.LogTrace("Publishing fire and forget event: {Envelope}", envelope);
                publisher.Publish(envelope, cancellationToken);
            }

            
            if (!denied.HasFlag(EventDeliveryMask.Awaited))
            {
                envelope = envelope with { DeliveryMode = EventDeliveryMode.Awaited };
                activity?.SetTag("event.publish.mode", EventDeliveryMask.Awaited.ToString("G"));
                _logger.LogTrace("Publishing awaited event: {Envelope}", envelope);
                await publisher.PublishAsync(envelope, cancellationToken);
            }
            activity?.SetTag("event.publish.mode", null);
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogTrace("Event publishing complete");
            _loggerFactory.CreateLogger(EventPublishLogCategory).LogDebug("Event: {EventType} - Published", eventType);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.ToString());
            _loggerFactory.CreateLogger(EventPublishLogCategory).LogWarning("Event {EventType} failed to publish: {ExceptionType} - {Exception}", eventType, ex.GetType().FullName, ex.Message);
            throw;
        }
    }

    public IDisposable Subscribe<T>(string? pattern, EventDeliveryMode mode, IEventHandler<T> handler, bool preserveSubscriberExecutionContext = false) where T : class
    {
        using var loggerScope = _logger.BeginScope("{EventTypePattern} {EventDataType} {EventMode} {EventHandlerType}", pattern, typeof(T), mode, handler.GetType());
        _logger.LogDebug("Adding subscription with pattern {EventTypePattern}", pattern);
        var handlerWrapper = ActivatorUtilities.CreateInstance<EventHandlerWrapper<T>>(_serviceProvider, handler, new EventHandlerWrapperOptions(pattern, mode, preserveSubscriberExecutionContext ? ExecutionContext.Capture() : null));
        var asyncSubscriber = _serviceProvider.GetRequiredService<IAsyncSubscriber<IEventEnvelope<T>>>();
        var subscription = asyncSubscriber.Subscribe(handlerWrapper);
        LogSubscribed(handler.GetType(), typeof(T), pattern);
        return new SubscriptionHandle(subscription, _logger, handler.GetType(), typeof(T), pattern);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Subscribed {EventHandlerType} to {EventType} with filter pattern {EventTypePattern}")]
    private partial void LogSubscribed(Type eventHandlerType, Type eventType, string? eventTypePattern);

    record EventEnvelope<T>(EventType Type, EventDeliveryMode DeliveryMode, Guid CorrelationId, T? Data) : IEventEnvelope<T> where T : class;
}
