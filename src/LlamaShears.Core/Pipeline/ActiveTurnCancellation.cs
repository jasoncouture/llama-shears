using LlamaShears.Core.Abstractions.Agent.Pipeline;

namespace LlamaShears.Core.Pipeline;

public sealed class ActiveTurnCancellation : IActiveTurnCancellation
{
    private readonly Lock _gate = new();
    private CancellationTokenSource? _current;

    /// <inheritdoc />
    public void Register(CancellationTokenSource cancellationTokenSource)
    {
        ArgumentNullException.ThrowIfNull(cancellationTokenSource);
        lock (_gate)
        {
            _current = cancellationTokenSource;
        }
    }

    /// <inheritdoc />
    public void Unregister(CancellationTokenSource cancellationTokenSource)
    {
        ArgumentNullException.ThrowIfNull(cancellationTokenSource);
        lock (_gate)
        {
            if (ReferenceEquals(_current, cancellationTokenSource))
            {
                _current = null;
            }
        }
    }

    /// <inheritdoc />
    public void Cancel()
    {
        CancellationTokenSource? source;
        lock (_gate)
        {
            source = _current;
        }

        source?.Cancel();
    }
}
