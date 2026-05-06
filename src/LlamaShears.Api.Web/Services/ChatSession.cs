using LlamaShears.Agent.Abstractions.Events;
using LlamaShears.Provider.Abstractions;
using MessagePipe;

namespace LlamaShears.Api.Web.Services;

/// <summary>
/// Per-circuit chat state. Holds the currently selected agent id, the
/// rolling bubble list, and the MessagePipe subscriptions that turn
/// agent-side events into UI updates. Switching the selected agent
/// disposes the existing subscriptions and re-subscribes filtered to
/// the new id.
/// </summary>
public sealed class ChatSession : IDisposable
{
    private readonly IAsyncSubscriber<AgentTurnEmitted> _turns;
    private readonly IAsyncSubscriber<AgentFragmentEmitted> _fragments;
    private readonly IAsyncPublisher<UserMessageSubmitted> _userMessages;
    private readonly List<ChatBubble> _bubbles = [];
    private readonly Dictionary<Guid, ChatBubble> _streamingBubbles = [];
    private readonly Lock _gate = new();
    private IDisposable? _turnSubscription;
    private IDisposable? _fragmentSubscription;
    private string? _selectedAgentId;

    public ChatSession(
        IAsyncSubscriber<AgentTurnEmitted> turns,
        IAsyncSubscriber<AgentFragmentEmitted> fragments,
        IAsyncPublisher<UserMessageSubmitted> userMessages)
    {
        _turns = turns;
        _fragments = fragments;
        _userMessages = userMessages;
    }

    public string? SelectedAgentId
    {
        get
        {
            lock (_gate)
            {
                return _selectedAgentId;
            }
        }
    }

    public IReadOnlyList<ChatBubble> Bubbles
    {
        get
        {
            lock (_gate)
            {
                return [.._bubbles];
            }
        }
    }

    /// <summary>
    /// Raised whenever the bubble list or a streaming bubble's content
    /// changes. Components subscribe and call <c>StateHasChanged</c> via
    /// <c>InvokeAsync</c> — events fire off the renderer's sync context.
    /// </summary>
    public event Action? Changed;

    public void SelectAgent(string? agentId)
    {
        lock (_gate)
        {
            if (string.Equals(_selectedAgentId, agentId, StringComparison.Ordinal))
            {
                return;
            }
            _selectedAgentId = agentId;
            _bubbles.Clear();
            _streamingBubbles.Clear();
            _turnSubscription?.Dispose();
            _fragmentSubscription?.Dispose();
            _turnSubscription = null;
            _fragmentSubscription = null;
            if (!string.IsNullOrWhiteSpace(agentId))
            {
                _turnSubscription = _turns.Subscribe(OnTurnAsync);
                _fragmentSubscription = _fragments.Subscribe(OnFragmentAsync);
            }
        }
        Changed?.Invoke();
    }

    public async Task SendAsync(string content, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        string agentId;
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(_selectedAgentId))
            {
                throw new InvalidOperationException("No agent selected.");
            }
            agentId = _selectedAgentId;
            var now = DateTimeOffset.UtcNow;
            _bubbles.Add(new ChatBubble(ChatBubbleKind.User, content, now));
        }
        Changed?.Invoke();
        await _userMessages.PublishAsync(
            new UserMessageSubmitted(agentId, content, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _turnSubscription?.Dispose();
            _fragmentSubscription?.Dispose();
            _turnSubscription = null;
            _fragmentSubscription = null;
        }
    }

    private ValueTask OnTurnAsync(AgentTurnEmitted evt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!string.Equals(evt.AgentId, _selectedAgentId, StringComparison.Ordinal))
            {
                return ValueTask.CompletedTask;
            }

            // Replace any still-streaming bubble of the same kind with
            // the authoritative content; otherwise append a fresh one.
            var kind = MapKind(evt.Turn.Role);
            if (kind is null)
            {
                return ValueTask.CompletedTask;
            }

            var streamingBubble = _bubbles.LastOrDefault(b =>
                b.Kind == kind && b.IsStreaming);
            if (streamingBubble is not null)
            {
                streamingBubble.Replace(evt.Turn.Content);
            }
            else
            {
                _bubbles.Add(new ChatBubble(kind.Value, evt.Turn.Content, evt.Turn.Timestamp));
            }
        }
        Changed?.Invoke();
        return ValueTask.CompletedTask;
    }

    private ValueTask OnFragmentAsync(AgentFragmentEmitted evt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!string.Equals(evt.AgentId, _selectedAgentId, StringComparison.Ordinal))
            {
                return ValueTask.CompletedTask;
            }

            if (!_streamingBubbles.TryGetValue(evt.StreamId, out var bubble))
            {
                if (evt.IsFinal)
                {
                    return ValueTask.CompletedTask;
                }
                var kind = evt.Kind == AgentFragmentKind.Thought
                    ? ChatBubbleKind.Thought
                    : ChatBubbleKind.Assistant;
                bubble = new ChatBubble(kind, string.Empty, DateTimeOffset.UtcNow, evt.StreamId);
                _streamingBubbles[evt.StreamId] = bubble;
                _bubbles.Add(bubble);
            }

            if (evt.IsFinal)
            {
                // Don't seal here. The eventual AgentTurnEmitted is the
                // authoritative source: it will replace the bubble's
                // content (correcting any drift between fragment
                // accumulation and the final string) and seal the bubble.
                // If we sealed here, OnTurnAsync would be unable to find
                // a streaming bubble and would append a duplicate.
                _streamingBubbles.Remove(evt.StreamId);
            }
            else if (!string.IsNullOrEmpty(evt.Delta))
            {
                bubble.AppendDelta(evt.Delta);
            }
        }
        Changed?.Invoke();
        return ValueTask.CompletedTask;
    }

    private static ChatBubbleKind? MapKind(ModelRole role) => role switch
    {
        ModelRole.Assistant => ChatBubbleKind.Assistant,
        ModelRole.FrameworkAssistant => ChatBubbleKind.Assistant,
        ModelRole.Thought => ChatBubbleKind.Thought,
        _ => null,
    };
}
