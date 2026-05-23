using System.Diagnostics;
using LlamaShears.Core.Abstractions.Events;
using MessagePipe;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Eventing;

internal sealed partial class EventHandlerWrapper<T> : IAsyncMessageHandler<IEventEnvelope<T>> where T : class
{
    private readonly IEventHandler<T> _handler;
    private readonly string? _pattern;
    private readonly EventDeliveryMode _deliveryMode;
    private readonly IPatternMatcher _patternMatcher;
    private readonly ILogger<EventBus> _logger;
    private readonly ExecutionContext? _executionContext;

    public EventHandlerWrapper(IEventHandler<T> handler, EventHandlerWrapperOptions options, IPatternMatcher patternMatcher, ILogger<EventBus> logger)
    {
        _handler = handler;
        _pattern = options.Pattern;
        _deliveryMode = options.DeliveryMode;
        _patternMatcher = patternMatcher;
        _logger = logger;
        _executionContext = options.ExecutionContext;
    }

    public async ValueTask HandleAsync(IEventEnvelope<T> envelope, CancellationToken cancellationToken)
    {
        if (envelope.DeliveryMode != _deliveryMode) return;
        if (!string.IsNullOrWhiteSpace(_pattern) && !_patternMatcher.IsMatch(_pattern, envelope.Type)) return;
        await Task.Yield();
        var activityContext = Activity.Current?.Context ?? default;
        ExecutionContext? currentContext = ExecutionContext.Capture()!;
        ExecutionContext.Restore(_executionContext ?? currentContext);

        using var activity = EventBus.ActivitySource.StartActivity($"event.consume {envelope.Type.Component}:{envelope.Type.EventName}", ActivityKind.Consumer, parentContext: activityContext);
        activity?.SetTag("event.id", envelope.Type.ToString());
        activity?.SetTag("event.payload.type", typeof(T).FullName);
        activity?.SetTag("consumer.type", _handler.GetType().FullName);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _handler.HandleAsync(envelope, cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok, "completed");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
            LogHandlerCancelled(_handler.GetType(), typeof(T), _deliveryMode);
        }
        finally
        {
            if (currentContext is not null)
                ExecutionContext.Restore(currentContext);
        }
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Handler {EventHandlerType} for {EventType} ({DeliveryMode}) observed cancellation; stopping.")]
    private partial void LogHandlerCancelled(Type eventHandlerType, Type eventType, EventDeliveryMode deliveryMode);
}
