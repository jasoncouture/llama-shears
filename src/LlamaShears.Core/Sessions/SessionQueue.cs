using System.Collections.Immutable;
using System.Threading.Channels;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Sessions;

internal sealed class SessionQueue : ISessionQueue, IAsyncDisposable
{
    private readonly Channel<ModelTurn> _userLane = Channel.CreateUnbounded<ModelTurn>(
        new UnboundedChannelOptions { SingleReader = false });

    private readonly Channel<ModelTurn> _toolLane = Channel.CreateUnbounded<ModelTurn>(
        new UnboundedChannelOptions { SingleReader = false });
    public bool HasQueuedMessages() => _userLane.Reader.Count > 0 || _toolLane.Reader.Count > 0;
    
    public ValueTask EnqueueAsync(ModelTurn turn, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turn);
        return turn.Role switch
        {
            ModelRole.User => _userLane.Writer.WriteAsync(turn, cancellationToken),
            ModelRole.Tool => _toolLane.Writer.WriteAsync(turn, cancellationToken),
            _ => throw new InvalidOperationException(
                $"SessionQueue only accepts User or Tool turns; got {turn.Role}."),
        };
    }

    public async ValueTask<ImmutableArray<ModelTurn>> DequeueBatchAsync(CancellationToken cancellationToken)
    {
        var tools = DrainAll(_toolLane.Reader);
        if (tools.Length > 0)
        {
            var users = TryDrainUserBatch(_userLane.Reader);
            return users.Length == 0 ? tools : [.. tools, .. users];
        }

        return await WaitForUserBatchAsync(cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        _userLane.Writer.TryComplete();
        _toolLane.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }

    private static ImmutableArray<ModelTurn> DrainAll(ChannelReader<ModelTurn> reader)
    {
        var builder = ImmutableArray.CreateBuilder<ModelTurn>();
        while (reader.TryRead(out var turn))
        {
            builder.Add(turn);
        }
        return builder.ToImmutable();
    }

    private static ImmutableArray<ModelTurn> TryDrainUserBatch(ChannelReader<ModelTurn> reader)
    {
        if (!reader.TryRead(out var first))
        {
            return [];
        }
        return DrainSameChannel(reader, first);
    }

    private async ValueTask<ImmutableArray<ModelTurn>> WaitForUserBatchAsync(CancellationToken cancellationToken)
    {
        var reader = _userLane.Reader;
        if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return [];
        }
        if (!reader.TryRead(out var first))
        {
            return [];
        }
        return DrainSameChannel(reader, first);
    }

    private static ImmutableArray<ModelTurn> DrainSameChannel(ChannelReader<ModelTurn> reader, ModelTurn first)
    {
        var batch = ImmutableArray.CreateBuilder<ModelTurn>();
        batch.Add(first);
        while (reader.TryPeek(out var next)
            && string.Equals(next.ChannelId, first.ChannelId, StringComparison.Ordinal))
        {
            if (!reader.TryRead(out var taken))
            {
                break;
            }
            batch.Add(taken);
        }
        return batch.ToImmutable();
    }
}
