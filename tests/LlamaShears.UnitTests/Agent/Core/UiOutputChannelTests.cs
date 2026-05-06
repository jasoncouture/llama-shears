using LlamaShears.Agent.Abstractions.Events;
using LlamaShears.Agent.Core.Channels;
using LlamaShears.Provider.Abstractions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class UiOutputChannelTests
{
    [Test]
    public async Task SendAsyncPublishesAnAgentTurnEmittedEventForTheOwningAgent()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<AgentTurnEmitted>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<AgentTurnEmitted>>();

        var captured = new List<AgentTurnEmitted>();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = subscriber.Subscribe((evt, _) =>
        {
            captured.Add(evt);
            gate.TrySetResult();
            return ValueTask.CompletedTask;
        });

        var channel = new UiOutputChannel("alice", publisher);
        var turn = new ModelTurn(ModelRole.Assistant, "hello back", DateTimeOffset.UtcNow);

        await channel.SendAsync(turn, CancellationToken.None);
        await gate.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await Assert.That(captured).Count().IsEqualTo(1);
        await Assert.That(captured[0].AgentId).IsEqualTo("alice");
        await Assert.That(captured[0].Turn).IsSameReferenceAs(turn);
    }

    [Test]
    public async Task ConstructorRejectsBlankAgentId()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<AgentTurnEmitted>>();

        await Assert.That(() => new UiOutputChannel("   ", publisher))
            .Throws<ArgumentException>();
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddMessagePipe();
        return services.BuildServiceProvider();
    }
}
