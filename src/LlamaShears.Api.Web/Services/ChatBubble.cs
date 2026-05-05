using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Content;

namespace LlamaShears.Api.Web.Services;

public sealed class ChatBubble
{
    private readonly List<ToolCallView>? _inFlightTools;

    private ChatBubble(
        ChatBubbleKind kind,
        string content,
        DateTimeOffset at,
        Guid? streamId,
        bool isStreaming,
        List<ToolCallView>? inFlightTools,
        ToolCallView? completedCall,
        bool isError,
        ImmutableArray<Attachment> attachments)
    {
        Kind = kind;
        Content = content;
        At = at;
        StreamId = streamId;
        IsStreaming = isStreaming;
        _inFlightTools = inFlightTools;
        CompletedCall = completedCall;
        IsError = isError;
        Attachments = attachments.IsDefault ? [] : attachments;
    }

    public ChatBubble(
        ChatBubbleKind kind,
        string content,
        DateTimeOffset at,
        Guid? streamId = null,
        ImmutableArray<Attachment> attachments = default)
        : this(kind, content, at, streamId, isStreaming: false, inFlightTools: null, completedCall: null, isError: false, attachments)
    {
    }

    public static ChatBubble ToolInFlight(Guid correlationId, DateTimeOffset at)
        => new(
            ChatBubbleKind.ToolInFlight,
            string.Empty,
            at,
            streamId: correlationId,
            isStreaming: true,
            inFlightTools: [],
            completedCall: null,
            isError: false,
            attachments: []);

    public static ChatBubble ToolResult(ToolCallView call, string result, bool isError, DateTimeOffset at)
        => new(
            ChatBubbleKind.ToolResult,
            result,
            at,
            streamId: null,
            isStreaming: false,
            inFlightTools: null,
            completedCall: call,
            isError: isError,
            attachments: []);

    public ChatBubbleKind Kind { get; }

    public string Content { get; private set; }

    public DateTimeOffset At { get; }

    public Guid? StreamId { get; }

    public bool IsStreaming { get; private set; }

    public IReadOnlyList<ToolCallView>? InFlightTools => _inFlightTools;

    public ToolCallView? CompletedCall { get; }

    public bool IsError { get; }

    public ImmutableArray<Attachment> Attachments { get; }

    public void Update(string content, bool streaming)
    {
        Content = content;
        IsStreaming = streaming;
    }

    public void AddInFlight(ToolCallView call)
    {
        _inFlightTools?.Add(call);
    }

    public bool RemoveInFlight(string callId)
    {
        if (_inFlightTools is null)
        {
            return false;
        }
        for (var i = 0; i < _inFlightTools.Count; i++)
        {
            if (string.Equals(_inFlightTools[i].CallId, callId, StringComparison.Ordinal))
            {
                _inFlightTools.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public int InFlightCount => _inFlightTools?.Count ?? 0;
}
