using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Agent.Core;

internal sealed class CapturingOutputChannel : IOutputChannel
{
    private readonly List<ModelTurn> _turns = [];
    private readonly TaskCompletionSource _firstTurn = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Lock _gate = new();

    public IReadOnlyList<ModelTurn> Turns
    {
        get
        {
            lock (_gate)
            {
                return [.._turns];
            }
        }
    }

    public Task SendAsync(ModelTurn turn, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _turns.Add(turn);
        }
        _firstTurn.TrySetResult();
        return Task.CompletedTask;
    }

    public async Task WaitForTurnAsync(TimeSpan timeout)
    {
        var completed = await Task.WhenAny(_firstTurn.Task, Task.Delay(timeout)).ConfigureAwait(false);
        if (completed != _firstTurn.Task)
        {
            throw new TimeoutException($"No turn captured within {timeout}.");
        }
    }
}
