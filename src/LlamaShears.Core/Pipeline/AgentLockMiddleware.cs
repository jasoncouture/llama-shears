using System.Diagnostics;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;

namespace LlamaShears.Core.Pipeline;

public sealed class AgentLockMiddleware : IAgentMiddleware
{
    private readonly IAgentLock _agentLock;

    public AgentLockMiddleware(IAgentLock agentLock)
    {
        _agentLock = agentLock;
    }

    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.AgentLock;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        using var lockScope = await _agentLock.AcquireLockAsync(cancellationToken);
        Activity.Current?.AddEvent(new ActivityEvent("lock acquired"));
        await next.Invoke(context, cancellationToken);
    }
}
