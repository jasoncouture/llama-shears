namespace LlamaShears.Api.Web.Services;

public sealed class ChatBubble
{
    public ChatBubble(ChatBubbleKind kind, string content, DateTimeOffset at, Guid? streamId = null)
    {
        Kind = kind;
        Content = content;
        At = at;
        StreamId = streamId;
    }

    public ChatBubbleKind Kind { get; }

    public string Content { get; private set; }

    public DateTimeOffset At { get; }

    public Guid? StreamId { get; }

    public bool IsStreaming { get; private set; }

    public void AppendDelta(string delta)
    {
        Content += delta;
        IsStreaming = true;
    }

    public void Replace(string content)
    {
        Content = content;
        IsStreaming = false;
    }

    public void Seal()
    {
        IsStreaming = false;
    }
}
