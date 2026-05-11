using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Eventing;

internal sealed partial class SubscriptionHandle : IDisposable
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
        LogUnsubscribed(_handlerType, _eventType, _pattern);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Unsubscribed {EventHandlerType} from {EventType} with filter pattern {EventTypePattern}")]
    private partial void LogUnsubscribed(Type eventHandlerType, Type eventType, string? eventTypePattern);
}
