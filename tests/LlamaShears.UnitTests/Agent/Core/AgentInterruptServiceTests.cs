using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.UnitTests.Agent.Pipeline;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentInterruptServiceTests
{
    [Test]
    public async Task HandleAsyncCancelsTheActiveTurn()
    {
        var slot = Substitute.For<IActiveTurnCancellation>();
        IAgentService service = new AgentInterruptService(
            Substitute.For<IEventBus>(),
            PipelineTestContext.ScopeFor(),
            slot);
        var envelope = Substitute.For<IEventEnvelope<AgentInterruptRequest>>();
        envelope.Data.Returns(AgentInterruptRequest.Instance);

        await ((IEventHandler<AgentInterruptRequest>)service).HandleAsync(envelope, CancellationToken.None);

        slot.Received(1).Cancel();
    }
}
