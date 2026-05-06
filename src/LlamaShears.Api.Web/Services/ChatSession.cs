using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Content;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Api.Web.Services;

public sealed class ChatSession :
    IChatSession,
    IEventHandler<AgentMessageFragment>,
    IEventHandler<AgentThoughtFragment>,
    IEventHandler<AgentToolCallFragment>,
    IEventHandler<AgentToolResultFragment>,
    IEventHandler<AgentCompactionMarker>
{
    private readonly IEventBus _bus;
    private readonly IEventPublisher _publisher;
    private readonly IAgentDirectory _directory;
    private readonly List<ChatBubble> _bubbles = [];
    private readonly Dictionary<(Guid CorrelationId, ChatBubbleKind Kind), ChatBubble> _streamingBubbles = [];
    private readonly Dictionary<Guid, ChatBubble> _inFlightToolBubbles = [];
    private readonly Lock _gate = new();
    private IDisposable? _messageSubscription;
    private IDisposable? _thoughtSubscription;
    private IDisposable? _toolCallSubscription;
    private IDisposable? _toolResultSubscription;
    private IDisposable? _compactingStartedSubscription;
    private IDisposable? _compactingFinishedSubscription;
    private string? _selectedAgentId;
    private bool _showThoughts = true;
    private bool _showStreaming = true;
    private bool _showTools = true;
    private bool _isCompacting;

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

    public bool ShowTools
    {
        get
        {
            lock (_gate)
            {
                return _showTools;
            }
        }
        set
        {
            lock (_gate)
            {
                if (_showTools == value)
                {
                    return;
                }
                _showTools = value;
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
            _inFlightToolBubbles.Clear();
            _isCompacting = false;
            _messageSubscription?.Dispose();
            _thoughtSubscription?.Dispose();
            _toolCallSubscription?.Dispose();
            _toolResultSubscription?.Dispose();
            _compactingStartedSubscription?.Dispose();
            _compactingFinishedSubscription?.Dispose();
            _messageSubscription = null;
            _thoughtSubscription = null;
            _toolCallSubscription = null;
            _toolResultSubscription = null;
            _compactingStartedSubscription = null;
            _compactingFinishedSubscription = null;
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
                _toolCallSubscription = _bus.Subscribe<AgentToolCallFragment>(
                    $"{Event.WellKnown.Agent.ToolCall}:{agentId}",
                    EventDeliveryMode.Awaited,
                    this);
                _toolResultSubscription = _bus.Subscribe<AgentToolResultFragment>(
                    $"{Event.WellKnown.Agent.ToolResult}:{agentId}",
                    EventDeliveryMode.Awaited,
                    this);
                _compactingStartedSubscription = _bus.Subscribe<AgentCompactionMarker>(
                    $"{Event.WellKnown.Agent.CompactingStarted}:{agentId}",
                    EventDeliveryMode.Awaited,
                    this);
                _compactingFinishedSubscription = _bus.Subscribe<AgentCompactionMarker>(
                    $"{Event.WellKnown.Agent.CompactingFinished}:{agentId}",
                    EventDeliveryMode.Awaited,
                    this);
            }
        }
        Changed?.Invoke();
    }

    public Task SendAsync(string content, CancellationToken cancellationToken)
        => SendAsync(content, attachments: [], cancellationToken);

    public async Task SendAsync(
        string content,
        ImmutableArray<Attachment> attachments,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var safeAttachments = attachments.IsDefault ? [] : attachments;
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
            _bubbles.Add(new ChatBubble(
                ChatBubbleKind.User,
                content,
                DateTimeOffset.UtcNow,
                attachments: safeAttachments));
        }
        Changed?.Invoke();
        await _publisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = "webui" },
            new ChannelMessage(content, agentId, DateTimeOffset.Now)
            {
                Attachments = safeAttachments,
            },
            cancellationToken).ConfigureAwait(false);
    }

    public bool IsCompacting
    {
        get
        {
            lock (_gate)
            {
                return _isCompacting;
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _messageSubscription?.Dispose();
            _thoughtSubscription?.Dispose();
            _toolCallSubscription?.Dispose();
            _toolResultSubscription?.Dispose();
            _compactingStartedSubscription?.Dispose();
            _compactingFinishedSubscription?.Dispose();
            _messageSubscription = null;
            _thoughtSubscription = null;
            _toolCallSubscription = null;
            _toolResultSubscription = null;
            _compactingStartedSubscription = null;
            _compactingFinishedSubscription = null;
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

    public ValueTask HandleAsync(IEventEnvelope<AgentToolCallFragment> envelope, CancellationToken cancellationToken)
    {
        if (envelope.Data is { } fragment)
        {
            ApplyToolCall(envelope.Type.Id, envelope.CorrelationId, fragment);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(IEventEnvelope<AgentToolResultFragment> envelope, CancellationToken cancellationToken)
    {
        if (envelope.Data is { } fragment)
        {
            ApplyToolResult(envelope.Type.Id, envelope.CorrelationId, fragment);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(IEventEnvelope<AgentCompactionMarker> envelope, CancellationToken cancellationToken)
    {
        var started = string.Equals(
            envelope.Type.Component,
            Event.WellKnown.Agent.CompactingStarted.Component,
            StringComparison.Ordinal)
            && string.Equals(
                envelope.Type.EventName,
                Event.WellKnown.Agent.CompactingStarted.EventName,
                StringComparison.Ordinal);
        ApplyCompactionState(envelope.Type.Id, started);
        return ValueTask.CompletedTask;
    }

    private void ApplyCompactionState(string? agentId, bool active)
    {
        bool changed;
        lock (_gate)
        {
            if (!string.Equals(agentId, _selectedAgentId, StringComparison.Ordinal))
            {
                return;
            }
            changed = _isCompacting != active;
            _isCompacting = active;
        }
        if (changed)
        {
            Changed?.Invoke();
        }
    }

    private void ApplyToolCall(string? agentId, Guid correlationId, AgentToolCallFragment fragment)
    {
        lock (_gate)
        {
            if (!string.Equals(agentId, _selectedAgentId, StringComparison.Ordinal))
            {
                return;
            }
            if (string.IsNullOrEmpty(fragment.CallId))
            {
                // Without an id we can't correlate the eventual result
                // back to this call. Drop it rather than render a bubble
                // we can never clear.
                return;
            }

            if (!_inFlightToolBubbles.TryGetValue(correlationId, out var bubble))
            {
                bubble = ChatBubble.ToolInFlight(correlationId, DateTimeOffset.UtcNow);
                _inFlightToolBubbles[correlationId] = bubble;
                _bubbles.Add(bubble);
            }
            bubble.AddInFlight(new ToolCallView(
                fragment.Source,
                fragment.Name,
                fragment.ArgumentsJson,
                fragment.CallId));
        }
        Changed?.Invoke();
    }

    private void ApplyToolResult(string? agentId, Guid correlationId, AgentToolResultFragment fragment)
    {
        lock (_gate)
        {
            if (!string.Equals(agentId, _selectedAgentId, StringComparison.Ordinal))
            {
                return;
            }
            if (string.IsNullOrEmpty(fragment.CallId))
            {
                return;
            }

            // Remove from the in-flight summary, then insert a result
            // bubble immediately ABOVE that summary so the running
            // indicator stays anchored at the bottom while completed
            // calls stack above it in completion order.
            var inFlightBubble = _inFlightToolBubbles.GetValueOrDefault(correlationId);
            var inFlightCall = inFlightBubble is not null
                ? FindInFlight(inFlightBubble, fragment.CallId)
                : null;
            inFlightBubble?.RemoveInFlight(fragment.CallId);

            var call = inFlightCall ?? new ToolCallView(
                fragment.Source,
                fragment.Name,
                ArgumentsJson: "{}",
                fragment.CallId);
            var resultBubble = ChatBubble.ToolResult(call, fragment.Result, fragment.IsError, DateTimeOffset.UtcNow);

            if (inFlightBubble is null)
            {
                _bubbles.Add(resultBubble);
            }
            else
            {
                var insertAt = _bubbles.IndexOf(inFlightBubble);
                if (insertAt < 0)
                {
                    _bubbles.Add(resultBubble);
                }
                else
                {
                    _bubbles.Insert(insertAt, resultBubble);
                }
                if (inFlightBubble.InFlightCount == 0)
                {
                    _bubbles.Remove(inFlightBubble);
                    _inFlightToolBubbles.Remove(correlationId);
                }
            }
        }
        Changed?.Invoke();
    }

    private static ToolCallView? FindInFlight(ChatBubble bubble, string callId)
    {
        if (bubble.InFlightTools is null)
        {
            return null;
        }
        foreach (var view in bubble.InFlightTools)
        {
            if (string.Equals(view.CallId, callId, StringComparison.Ordinal))
            {
                return view;
            }
        }
        return null;
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
            case ChatCommand.Compact:
                await _directory.RequestCompactionAsync(agentId, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private void ResetBubbles()
    {
        lock (_gate)
        {
            _bubbles.Clear();
            _streamingBubbles.Clear();
            _inFlightToolBubbles.Clear();
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
        if (string.Equals(trimmedContent, "/compact", StringComparison.OrdinalIgnoreCase))
        {
            command = ChatCommand.Compact;
            return true;
        }
        return false;
    }

    private static ChatBubble? HistoryBubbleFromTurn(ModelTurn turn)
    {
        // Live turn-arrival is the source of truth for Assistant/Thought;
        // for history backfill we additionally render User turns so the
        // user sees their side of the prior conversation, and Tool turns
        // so prior tool activity replays as result bubbles. System and
        // framework-injected turns stay hidden — they're prompt plumbing,
        // not chat content.
        if (turn.Role == ModelRole.Tool)
        {
            // Older persisted Tool turns may pre-date the ToolCall field;
            // skip those rather than render a bubble with no header
            // anchor.
            if (turn.ToolCall is not { } persistedCall)
            {
                return null;
            }
            var view = new ToolCallView(
                persistedCall.Source,
                persistedCall.Name,
                persistedCall.ArgumentsJson,
                persistedCall.CallId ?? string.Empty);
            return ChatBubble.ToolResult(view, turn.Content, turn.IsError, turn.Timestamp);
        }

        var kind = turn.Role switch
        {
            ModelRole.User => ChatBubbleKind.User,
            ModelRole.Assistant => ChatBubbleKind.Assistant,
            ModelRole.Thought => ChatBubbleKind.Thought,
            _ => (ChatBubbleKind?)null,
        };
        if (kind is null)
        {
            return null;
        }
        // Empty-content assistant/user/thought turns are skipped UNLESS
        // they carry attachments — an image-only message has empty text
        // by design and we still want to render the picture.
        var hasContent = !string.IsNullOrEmpty(turn.Content);
        var hasAttachments = !turn.Attachments.IsDefaultOrEmpty;
        if (!hasContent && !hasAttachments)
        {
            return null;
        }
        return new ChatBubble(
            kind.Value,
            turn.Content,
            turn.Timestamp,
            attachments: turn.Attachments);
    }

    private enum ChatCommand
    {
        Clear,
        Archive,
        Compact,
    }
}
