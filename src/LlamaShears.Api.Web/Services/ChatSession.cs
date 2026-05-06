using LlamaShears.Core.Abstractions.Agent.Events;
using LlamaShears.Core.Abstractions.Provider;
using MessagePipe;

namespace LlamaShears.Api.Web.Services;

public sealed class ChatSession : IChatSession
{
    private readonly IAsyncSubscriber<AgentTurnEmitted> _turns;
    private readonly IAsyncSubscriber<AgentFragmentEmitted> _fragments;
    private readonly IAsyncPublisher<UserMessageSubmitted> _userMessages;
    private readonly IAgentDirectory _directory;
    private readonly List<ChatBubble> _bubbles = [];
    private readonly Dictionary<Guid, ChatBubble> _streamingBubbles = [];
    private readonly Lock _gate = new();
    private IDisposable? _turnSubscription;
    private IDisposable? _fragmentSubscription;
    private string? _selectedAgentId;
    private bool _showThoughts = true;
    private bool _showStreaming = true;

    public ChatSession(
        IAsyncSubscriber<AgentTurnEmitted> turns,
        IAsyncSubscriber<AgentFragmentEmitted> fragments,
        IAsyncPublisher<UserMessageSubmitted> userMessages,
        IAgentDirectory directory)
    {
        _turns = turns;
        _fragments = fragments;
        _userMessages = userMessages;
        _directory = directory;
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

    public bool ShowThoughts
    {
        get
        {
            lock (_gate)
            {
                return _showThoughts;
            }
        }
        set
        {
            lock (_gate)
            {
                if (_showThoughts == value)
                {
                    return;
                }
                _showThoughts = value;
            }
            Changed?.Invoke();
        }
    }

    public bool ShowStreaming
    {
        get
        {
            lock (_gate)
            {
                return _showStreaming;
            }
        }
        set
        {
            lock (_gate)
            {
                if (_showStreaming == value)
                {
                    return;
                }
                _showStreaming = value;
            }
            Changed?.Invoke();
        }
    }

    public event Action? Changed;

    public async Task SelectAgentAsync(string? agentId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ModelTurn> history = [];
        if (!string.IsNullOrWhiteSpace(agentId))
        {
            history = await _directory.GetTurnsAsync(agentId, cancellationToken).ConfigureAwait(false);
        }

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
                foreach (var turn in history)
                {
                    var bubble = HistoryBubbleFromTurn(turn);
                    if (bubble is not null)
                    {
                        _bubbles.Add(bubble);
                    }
                }
                _turnSubscription = _turns.Subscribe(OnTurnAsync);
                _fragmentSubscription = _fragments.Subscribe(OnFragmentAsync);
            }
        }
        Changed?.Invoke();
    }

    public async Task SendAsync(string content, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var trimmed = content.Trim();
        string agentId;
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(_selectedAgentId))
            {
                throw new InvalidOperationException("No agent selected.");
            }
            agentId = _selectedAgentId;
        }

        if (TryParseCommand(trimmed, out var command))
        {
            await ExecuteCommandAsync(agentId, command, cancellationToken).ConfigureAwait(false);
            return;
        }

        lock (_gate)
        {
            _bubbles.Add(new ChatBubble(ChatBubbleKind.User, content, DateTimeOffset.UtcNow));
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

    private async Task ExecuteCommandAsync(string agentId, ChatCommand command, CancellationToken cancellationToken)
    {
        switch (command)
        {
            case ChatCommand.Clear:
                await _directory.ClearAsync(agentId, archive: false, cancellationToken).ConfigureAwait(false);
                ResetBubbles();
                break;
            case ChatCommand.Archive:
                await _directory.ClearAsync(agentId, archive: true, cancellationToken).ConfigureAwait(false);
                ResetBubbles();
                break;
        }
    }

    private void ResetBubbles()
    {
        lock (_gate)
        {
            _bubbles.Clear();
            _streamingBubbles.Clear();
        }
        Changed?.Invoke();
    }

    private static bool TryParseCommand(string trimmedContent, out ChatCommand command)
    {
        command = default;
        if (trimmedContent.Length < 2 || trimmedContent[0] != '/')
        {
            return false;
        }
        if (string.Equals(trimmedContent, "/clear", StringComparison.OrdinalIgnoreCase))
        {
            command = ChatCommand.Clear;
            return true;
        }
        if (string.Equals(trimmedContent, "/archive", StringComparison.OrdinalIgnoreCase))
        {
            command = ChatCommand.Archive;
            return true;
        }
        return false;
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

    private static ChatBubble? HistoryBubbleFromTurn(ModelTurn turn)
    {
        // Live turn-arrival is the source of truth for Assistant/Thought;
        // for history backfill we additionally render User turns so the
        // user sees their side of the prior conversation. System and
        // framework-injected turns stay hidden — they're prompt plumbing,
        // not chat content.
        var kind = turn.Role switch
        {
            ModelRole.User => ChatBubbleKind.User,
            ModelRole.Assistant => ChatBubbleKind.Assistant,
            ModelRole.Thought => ChatBubbleKind.Thought,
            _ => (ChatBubbleKind?)null,
        };
        return kind is null ? null : new ChatBubble(kind.Value, turn.Content, turn.Timestamp);
    }

    private enum ChatCommand
    {
        Clear,
        Archive,
    }
}
