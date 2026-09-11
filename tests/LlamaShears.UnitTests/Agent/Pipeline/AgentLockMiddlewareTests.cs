using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Pipeline;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class AgentLockMiddlewareTests
{
    [Test]
    public async Task HoldsTheSessionLockAcrossNext()
    {
        var manager = new AgentLockManager();
        var scope = PipelineTestContext.ScopeFor();
        var sessionKey = scope.GetCurrentSessionId().ToString();
        IAgentLock agentLock = new AgentLock(manager, scope);
        IAgentMiddleware middleware = new AgentLockMiddleware(agentLock);
        Task<ILockScope>? pending = null;

        await middleware.InvokeAsync(
            PipelineTestContext.Create(),
            async (_, _) =>
            {
                pending = manager.AcquireLockAsync(sessionKey, CancellationToken.None).AsTask();
                await Task.Delay(50);
                await Assert.That(pending.IsCompleted).IsFalse();
            },
            CancellationToken.None);

        using var next = await pending!.WaitAsync(TimeSpan.FromMilliseconds(500));
        await Assert.That(next.Active).IsTrue();
    }
}
