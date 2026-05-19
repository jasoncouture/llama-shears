using System.ComponentModel;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Session;

[McpServerToolType]
public sealed partial class SessionReplyTool
{
    private readonly IDataContextScope _dataScope;
    private readonly IEventPublisher _eventPublisher;
    private readonly TimeProvider _time;
    private readonly ILogger<SessionReplyTool> _logger;

    public SessionReplyTool(
        IDataContextScope dataScope,
        IEventPublisher eventPublisher,
        TimeProvider time,
        ILogger<SessionReplyTool> logger)
    {
        _dataScope = dataScope;
        _eventPublisher = eventPublisher;
        _time = time;
        _logger = logger;
    }

    [McpServerTool(Name = "session_reply")]
    [Description("Sends a message to the calling (parent) session. The ephemeral session continues running after this call; call it multiple times to send multiple messages, or omit it entirely to let the system send your final assistant message as the reply. Returns Sent=true on success, Sent=false with an Error string if invoked from a non-ephemeral context.")]
    public async Task<SessionReplyResult> SessionReplyAsync(
        [Description("Message to send to the calling (parent) session.")] string message,
        CancellationToken cancellationToken)
    {
        if (!_dataScope.TryGetValue<EphemeralSessionContext>(EphemeralSessionContext.DataKey, out var sessionContext)
            || sessionContext is null)
        {
            return new SessionReplyResult(Sent: false, Error: "not in an ephemeral session");
        }

        if (string.IsNullOrEmpty(message))
        {
            return new SessionReplyResult(Sent: false, Error: "message is required");
        }

        await _eventPublisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = sessionContext.ChannelId },
            new ChannelMessage(message, sessionContext.Parent.AgentId, _time.GetLocalNow())
            {
                SessionId = sessionContext.SessionId,
            },
            cancellationToken);
        sessionContext.ReplySent = true;
        LogReplyPublished(sessionContext.Parent.AgentId, sessionContext.SessionId, message.Length);
        return new SessionReplyResult(Sent: true);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "session_reply published {Bytes} chars to parent agent '{AgentId}' from ephemeral session '{SessionId}'.")]
    private partial void LogReplyPublished(string agentId, Guid sessionId, int bytes);
}
