using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using LlamaShears.Core.Persistence;
using LlamaShears.Core.Sessions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentLoopTests
{
    [Test]
    public async Task ChannelMessageDrivesAResponseIntoContextAndOutputs()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventBus>();
        var bus = provider.GetRequiredService<IEventBus>();
        var session = new SessionId("alice", SessionId.DefaultSessionName);
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync(session, CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, session);
        var model = new ScriptedLanguageModel("hi back");

        await using var agent = await BuildAgent("alice", session, provider, ctx, model);

        await PublishChannelMessageAsync(publisher, session, "hello");

        await captured.WaitForTurnAsync(TimeSpan.FromMilliseconds(500));

        await Assert.That(captured.Turns).Count().IsEqualTo(1);
        await Assert.That(captured.Turns[0].Role).IsEqualTo(ModelRole.Assistant);
        await Assert.That(captured.Turns[0].Content).IsEqualTo("hi back");
    }

    [Test]
    public async Task ChannelMessageTargetedAtAnotherSessionIsIgnored()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventBus>();
        var bus = provider.GetRequiredService<IEventBus>();
        var session = new SessionId("alice", SessionId.DefaultSessionName);
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync(session, CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, session);
        var model = new ScriptedLanguageModel("should not appear");

        await using var agent = await BuildAgent("alice", session, provider, ctx, model);

        var otherSession = new SessionId("bob", SessionId.DefaultSessionName);
        await PublishChannelMessageAsync(publisher, otherSession, "not for alice");

        await Assert.That(model.PromptInvocations).IsEqualTo(0);
        await Assert.That(captured.Turns).IsEmpty();
    }

    [Test]
    public async Task UserMessageInvokesMemorySearcherOnceAndStillProducesTurn()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventBus>();
        var bus = provider.GetRequiredService<IEventBus>();
        var session = new SessionId("alice", SessionId.DefaultSessionName);
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync(session, CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, session);
        var model = new ScriptedLanguageModel("hi back");
        var memorySearcher = Substitute.For<IMemorySearcher>();
        memorySearcher
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<double?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyList<MemorySearchResult>>([]));

        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice") with
        {
            WorkspacePath = Path.Combine(Path.GetTempPath(), $"memory-search-{Guid.NewGuid():N}"),
        };

        await using var agent = await BuildAgent("alice", session, provider, ctx, model, config, memorySearcher);

        await PublishChannelMessageAsync(publisher, session, "hello");

        await captured.WaitForTurnAsync(TimeSpan.FromMilliseconds(500));

        await Assert.That(captured.Turns).Count().IsEqualTo(1);
        await Assert.That(captured.Turns[0].Role).IsEqualTo(ModelRole.Assistant);
        await memorySearcher
            .Received(1)
            .SearchAsync(
                "alice",
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<double?>(),
                Arg.Any<CancellationToken>());
    }

    private const string TestChannelId = "test";

    private static ValueTask PublishChannelMessageAsync(
        IEventBus publisher,
        SessionId session,
        string text)
        => publisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = session },
            new ChannelMessage(text, TestChannelId, DateTimeOffset.UtcNow),
            CancellationToken.None);

    private static Task<LlamaShears.Core.Agent> BuildAgent(
        string id,
        SessionId session,
        IServiceProvider services,
        IAgentContext agentContext,
        ILanguageModel model,
        AgentConfig? config = null,
        IMemorySearcher? memorySearcher = null)
        => AgentHarness.StartAsync(
            id,
            session,
            services,
            agentContext,
            model,
            config: config,
            memorySearcher: memorySearcher);

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
        services.AddSingleton<IContextStore>(new FakeContextStore());
        services.AddSingleton(Substitute.For<IDataContextFactory>());
        services.AddEventHandler<AgentTurnContextPersister>();
        services.AddSingleton<ISessionFactory, SessionFactory>();
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AgentTurnContextPersister>();
        return provider;
    }
}
