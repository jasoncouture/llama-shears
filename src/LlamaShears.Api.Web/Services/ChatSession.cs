using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Api.Web.Services;

public sealed class ChatSession :
    IChatSession,
    IEventHandler<AgentMessageFragment>,
    IEventHandler<AgentThoughtFragment>
{
    private readonly IEventBus _bus;
    private readonly IEventPublisher _publisher;
    private readonly IAgentDirectory _directory;
    private readonly List<ChatBubble> _bubbles = [];
    private readonly Dictionary<(Guid CorrelationId, ChatBubbleKind Kind), ChatBubble> _streamingBubbles = [];
    private readonly Lock _gate = new();
    private IDisposable? _messageSubscription;
    private IDisposable? _thoughtSubscription;
    private string? _selectedAgentId;
    private bool _showThoughts = true;
    private bool _showStreaming = true;

    public ChatSession(
        IEventBus bus,
        IEventPublisher publisher,
        IAgentDirectory directory)
    {
        _bus = bus;
        _publisher = publisher;
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
                return [.. _bubbles];
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
            _messageSubscription?.Dispose();
            _thoughtSubscription?.Dispose();
            _messageSubscription = null;
            _thoughtSubscription = null;
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
                _messageSubscription = _bus.Subscribe<AgentMessageFragment>(
                    $"{Event.WellKnown.Agent.Message}:{agentId}",
                    EventDeliveryMode.Awaited,
                    this);
                _thoughtSubscription = _bus.Subscribe<AgentThoughtFragment>(
                    $"{Event.WellKnown.Agent.Thought}:{agentId}",
                    EventDeliveryMode.Awaited,
                    this);
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
        await _publisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = "webui" },
            new ChannelMessage(content, agentId, DateTimeOffset.Now),
            cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _messageSubscription?.Dispose();
            _thoughtSubscription?.Dispose();
            _messageSubscription = null;
            _thoughtSubscription = null;
        }
    }

    public ValueTask HandleAsync(IEventEnvelope<AgentMessageFragment> envelope, CancellationToken cancellationToken)
    {
        if (envelope.Data is { } fragment)
        {
            ApplyFragment(envelope.Type.Id, envelope.CorrelationId, ChatBubbleKind.Assistant, fragment.Content, fragment.Final);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(IEventEnvelope<AgentThoughtFragment> envelope, CancellationToken cancellationToken)
    {
        if (envelope.Data is { } fragment)
        {
            ApplyFragment(envelope.Type.Id, envelope.CorrelationId, ChatBubbleKind.Thought, fragment.Content, fragment.Final);
        }
        return ValueTask.CompletedTask;
    }

    private void ApplyFragment(string? agentId, Guid correlationId, ChatBubbleKind kind, string content, bool final)
    {
        lock (_gate)
        {
            if (!string.Equals(agentId, _selectedAgentId, StringComparison.Ordinal))
            {
                return;
            }

            var key = (correlationId, kind);
            if (!_streamingBubbles.TryGetValue(key, out var bubble))
            {
                bubble = new ChatBubble(kind, content, DateTimeOffset.UtcNow, correlationId);
                _streamingBubbles[key] = bubble;
                _bubbles.Add(bubble);
            }
            bubble.Update(content, streaming: !final);
            if (final)
            {
                _streamingBubbles.Remove(key);
            }
        }
        Changed?.Invoke();
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

    private static ChatBubble? HistoryBubbleFromTurn(ModelTurn turn)
    {
        // Live turn-arrival is the source of truth for Assistant/Thought;
        // for history backfill we additionally render User turns so the
        // user sees their side of the prior conversation. System,
        // framework-injected, and Tool turns stay hidden — they're prompt
        // plumbing, not chat content. Tool calls and results have their
        // own event channels for any future UI rendering.
        var kind = turn.Role switch
        {
            ModelRole.User => ChatBubbleKind.User,
            ModelRole.Assistant => ChatBubbleKind.Assistant,
            ModelRole.Thought => ChatBubbleKind.Thought,
            _ => (ChatBubbleKind?)null,
        };
        if (kind is null || string.IsNullOrEmpty(turn.Content))
        {
            // Empty-content assistant turns are real: a model that
            // responds with only tool calls leaves Content empty and
            // ToolCalls populated. The bubble would be a blank rectangle;
            // skip it.
            return null;
        }
        return new ChatBubble(kind.Value, turn.Content, turn.Timestamp);
    }

    private enum ChatCommand
    {
        Clear,
        Archive,
    }
}
