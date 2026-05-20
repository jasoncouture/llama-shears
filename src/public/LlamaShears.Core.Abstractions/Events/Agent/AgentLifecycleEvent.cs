using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Payload carried by agent lifecycle events (<c>agent:starting</c>, <c>agent:started</c>, <c>agent:stopping</c>,
/// <c>agent:stopped</c>) identifying which agent boot the notification refers to.
/// </summary>
/// <param name="Config">Config the agent was started with.</param>
/// <param name="SessionId">Session id of the boot — distinguishes the default (main) session from sub-sessions of the same agent.</param>
public sealed record AgentLifecycleEvent(AgentConfig Config, SessionId SessionId);
