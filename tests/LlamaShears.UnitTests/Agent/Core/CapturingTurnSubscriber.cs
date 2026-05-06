using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Agent.Core;

/// <summary>
/// Subscribes to <c>agent:turn:&lt;id&gt;</c> for a single agent and
/// captures every non-User turn (i.e. everything an old IOutputChannel
/// would have seen). Used as a drop-in replacement for the old
/// CapturingOutputChannel test helper.
/// </summary>
internal sealed class CapturingTurnSubscriber : IEventHandler<ModelTurn>, IDisposable
{
    private readonly List<ModelTurn> _turns = [];
    private readonly Lock _gate = new();
    private readonly TaskCompletionSource _firstTurn = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IDisposable _subscription;

    public CapturingTurnSubscriber(IEventBus bus, string agentId)
    {
        _subscription = bus.Subscribe<ModelTurn>(
            $"{Event.WellKnown.Agent.Turn}:{agentId}",
            EventDeliveryMode.Awaited,
            this);
    }

    public IReadOnlyList<ModelTurn> Turns
    {
        get
        {
            lock (_gate)
            {
                return [.. _turns];
            }
        }
    }

    public ValueTask HandleAsync(IEventEnvelope<ModelTurn> envelope, CancellationToken cancellationToken)
    {
        var turn = envelope.Data;
        if (turn is null || turn.Role == ModelRole.User)
        {
            return ValueTask.CompletedTask;
        }
        lock (_gate)
        {
            _turns.Add(turn);
        }
        _firstTurn.TrySetResult();
        return ValueTask.CompletedTask;
    }

    public async Task WaitForTurnAsync(TimeSpan timeout)
    {
        var completed = await Task.WhenAny(_firstTurn.Task, Task.Delay(timeout)).ConfigureAwait(false);
        if (completed != _firstTurn.Task)
        {
            throw new TimeoutException($"No turn captured within {timeout}.");
        }
    }

    public void Dispose() => _subscription.Dispose();
}
