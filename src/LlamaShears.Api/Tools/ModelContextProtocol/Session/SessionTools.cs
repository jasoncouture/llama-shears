using System.Collections.Immutable;
using System.ComponentModel;
using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Session;

[McpServerToolType]
public sealed class SessionTools
{
    private readonly IDataContextScope _dataScope;
    private readonly IAgentInstanceRepository _instances;
    private readonly IEventBus _eventBus;
    private readonly TimeProvider _time;

    public SessionTools(
        IDataContextScope dataScope,
        IAgentInstanceRepository instances,
        IEventBus eventBus,
        TimeProvider time)
    {
        _dataScope = dataScope;
        _instances = instances;
        _eventBus = eventBus;
        _time = time;
    }

    [McpServerTool(Name = "session_list", Destructive = false, OpenWorld = false, ReadOnly = true)]
    [Description("Lists every live session that belongs to the calling agent. Returns a JSON object with sessionCount plus an array of sessions; each entry carries the session id (canonical form `agentId:guid:name`), the session name (e.g. `default`, `heartbeat`, `cron-<jobid>`), and an isDefault flag. Sessions owned by other agents are never returned — there is no cross-agent visibility through this tool. Use the id from this list as the target of `session_send`.")]
    public SessionListResult ListSessions(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var agentId = _dataScope.GetAgentConfig().Id;
        var sessions = _instances.GetAllAgents()
            .Where(handle => string.Equals(handle.SessionPath.Current.AgentId, agentId, StringComparison.Ordinal))
            .Select(handle => new SessionEntry(
                Id: handle.SessionPath.Current.ToString(),
                Name: handle.SessionPath.Current.Name,
                IsDefault: handle.SessionPath.Current.IsDefault))
            .OrderByDescending(entry => entry.IsDefault)
            .ThenBy(entry => entry.Name, StringComparer.Ordinal)
            .ToImmutableArray();
        return new SessionListResult(SessionCount: sessions.Length, Sessions: sessions);
    }

    [McpServerTool(Name = "session_send", Destructive = false, OpenWorld = false)]
    [Description("Sends a message to one of the calling agent's live sessions. Same delivery path as a user-typed chat message arriving at the main agent: the target session sees the text as a new user-role turn and runs an inference turn on it. Useful for a transient sub-agent (heartbeat, cron) to forward a concise result to the parent agent's default session. Refuses if the target session belongs to a different agent, or if `sessionId` is not a parseable canonical id. Returns a JSON object with the parsed sessionId and a sent flag.")]
    public async Task<SessionSendResult> SendSession(
        [Description("Target session id in canonical `agentId:guid:name` form, as returned by `session_list`. Must belong to the calling agent.")]
        string sessionId,
        [Description("Message text to deliver as a user-role turn to the target session.")]
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new SessionSendResult(SessionId: null, Sent: false, Error: "Refused: sessionId is required.");
        }
        if (string.IsNullOrWhiteSpace(message))
        {
            return new SessionSendResult(SessionId: sessionId, Sent: false, Error: "Refused: message is required.");
        }
        if (!SessionId.TryParse(sessionId, out var target))
        {
            return new SessionSendResult(SessionId: sessionId, Sent: false, Error: $"Refused: '{sessionId}' is not a valid session id.");
        }

        var callerAgentId = _dataScope.GetAgentConfig().Id;
        if (!string.Equals(target.AgentId, callerAgentId, StringComparison.Ordinal))
        {
            return new SessionSendResult(SessionId: sessionId, Sent: false, Error: $"Refused: session '{sessionId}' does not belong to agent '{callerAgentId}'. Cross-agent messaging is not allowed.");
        }

        var callerSession = _dataScope.GetSessionPath().Current;
        var payload = new ChannelMessage(message, $"subagent:{callerSession}", _time.GetLocalNow());
        await _eventBus.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = target },
            payload,
            cancellationToken);
        return new SessionSendResult(SessionId: target.ToString(), Sent: true);
    }
}
