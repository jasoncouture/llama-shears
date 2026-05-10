namespace LlamaShears.Core.Abstractions.Events;

internal sealed class DelegateEventHandler<T> : IEventHandler<T>, IDisposable
    where T : class
{
    private readonly Func<IEventEnvelope<T>, CancellationToken, ValueTask> _handler;
    public IDisposable? SubscriptionHandle { get; set; }


    public DelegateEventHandler(Func<IEventEnvelope<T>, CancellationToken, ValueTask> handler)
    {
        _handler = handler;
    }

    public ValueTask HandleAsync(IEventEnvelope<T> envelope, CancellationToken cancellationToken)
        => _handler.Invoke(envelope, cancellationToken);

    public void Dispose()
    {
        SubscriptionHandle?.Dispose();
        SubscriptionHandle = null;
    }
}
