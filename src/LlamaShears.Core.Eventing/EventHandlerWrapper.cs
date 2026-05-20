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
        ExecutionContext? currentContext = null;
        if (_executionContext is not null)
        {
            currentContext = ExecutionContext.Capture();
            ExecutionContext.Restore(_executionContext);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _handler.HandleAsync(envelope, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
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
