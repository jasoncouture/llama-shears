using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Events;
using LlamaShears.Core.Abstractions.Provider;
using MessagePipe;

namespace LlamaShears.Core.Channels;

/// <summary>
/// Input channel backed by <see cref="UserMessageSubmitted"/> events on
/// the in-process bus. Buffers messages while the agent is busy and, on
/// the next read, yields exactly one <see cref="ModelTurn"/> — either
/// the original message verbatim (when only one is queued) or a
/// coalesced message that lists every queued entry in arrival order.
/// </summary>
public sealed class UiInputChannel : IInputChannel, IDisposable
{
    private readonly string _agentId;
    private readonly Queue<UserMessageSubmitted> _pending = new();
    private readonly Lock _gate = new();
    private readonly IDisposable _subscription;
    private TaskCompletionSource _signal = NewSignal();

    public UiInputChannel(string agentId, IAsyncSubscriber<UserMessageSubmitted> subscriber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentNullException.ThrowIfNull(subscriber);
        _agentId = agentId;
        _subscription = subscriber.Subscribe(OnMessageAsync);
    }

    public void Dispose()
    {
        _subscription.Dispose();
        lock (_gate)
        {
            _signal.TrySetResult();
            _pending.Clear();
        }
    }

    public async IAsyncEnumerable<ModelTurn> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        UserMessageSubmitted[] drained;
        lock (_gate)
        {
            if (_pending.Count == 0)
            {
                yield break;
            }
            drained = [.._pending];
            _pending.Clear();
            _signal = NewSignal();
        }

        cancellationToken.ThrowIfCancellationRequested();
        yield return drained.Length == 1
            ? new ModelTurn(ModelRole.User, drained[0].Content, drained[0].At)
            : new ModelTurn(ModelRole.User, Coalesce(drained), drained[^1].At);
        await Task.CompletedTask;
    }

    public Task WaitForInputAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource current;
        lock (_gate)
        {
            if (_pending.Count > 0)
            {
                return Task.CompletedTask;
            }
            current = _signal;
        }
        return current.Task.WaitAsync(cancellationToken);
    }

    private ValueTask OnMessageAsync(UserMessageSubmitted message, CancellationToken cancellationToken)
    {
        if (!string.Equals(message.AgentId, _agentId, StringComparison.Ordinal))
        {
            return ValueTask.CompletedTask;
        }

        TaskCompletionSource toSignal;
        lock (_gate)
        {
            _pending.Enqueue(message);
            toSignal = _signal;
        }
        toSignal.TrySetResult();
        return ValueTask.CompletedTask;
    }

    private static string Coalesce(IReadOnlyList<UserMessageSubmitted> messages)
    {
        var preamble = string.Format(
            CultureInfo.InvariantCulture,
            "The following {0} messages arrived since your last response, in order:",
            messages.Count);

        var builder = new StringBuilder();
        builder.Append(preamble);
        for (var i = 0; i < messages.Count; i++)
        {
            builder.Append("\n\n[");
            builder.Append((i + 1).ToString(CultureInfo.InvariantCulture));
            builder.Append("] (");
            builder.Append(messages[i].At.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            builder.Append(") ");
            builder.Append(messages[i].Content);
        }
        return builder.ToString();
    }

    private static TaskCompletionSource NewSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
