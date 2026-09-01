using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;

namespace LlamaShears.UnitTests.Agent.Core;

internal static class AgentStartHelper
{
    public static async Task StartAndWaitAsync(IEventBus bus, SessionId session, IAgent agent)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = bus.Subscribe<AgentLifecycleEvent>(
            Event.WellKnown.Agent.Started with { Id = session },
            EventDeliveryMode.Awaited,
            (_, _) =>
            {
                started.TrySetResult();
                return ValueTask.CompletedTask;
            });
        _ = agent.RunAsync();
        await started.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
    }
}
