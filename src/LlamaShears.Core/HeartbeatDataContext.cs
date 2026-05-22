using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.SystemPrompt;

namespace LlamaShears.Core;

public sealed record HeartbeatDataContext(SessionId Session, SessionId Parent, WorkspaceFile HeartbeatFile)
{
    public const string DataKey = "heartbeat";
}
