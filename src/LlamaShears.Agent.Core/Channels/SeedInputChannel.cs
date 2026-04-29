using System.Runtime.CompilerServices;
using LlamaShears.Agent.Abstractions;
using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Core.Channels;

public sealed class SeedInputChannel : IInputChannel
{
    private readonly Queue<ModelTurn> _pending;

    public SeedInputChannel(IEnumerable<ModelTurn> seed)
    {
        _pending = new Queue<ModelTurn>(seed);
    }

    public async IAsyncEnumerable<ModelTurn> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (_pending.TryDequeue(out var turn))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return turn;
            await Task.Yield();
        }
    }
}
