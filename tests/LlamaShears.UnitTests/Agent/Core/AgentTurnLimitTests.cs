using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using LlamaShears.Core.Persistence;
using LlamaShears.Core.Sessions;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentTurnLimitTests
{
    private const string TestChannelId = "test";

    [Test]
    public async Task TurnLimitTwoStopsAfterTwoInfersAndOneDispatch()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventBus>();
        var session = new SessionId("alice", SessionId.DefaultSessionName);
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync(session, CancellationToken.None);
        var model = new ToolThenTextModel(toolRounds: 4);
        var dispatcher = Substitute.For<IToolCallDispatcher>();
        dispatcher
            .DispatchAsync(
                Arg.Any<ToolCall>(),
                Arg.Any<ImmutableArray<ToolGroup>>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ToolCallResult>(new ToolCallResult("ok", IsError: false)));
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice") with
        {
            Tools = new AgentToolConfig { TurnLimit = 2 },
        };
        using var idle = new IdleWaiter(publisher, session);

        await using var agent = await AgentHarness.StartAsync(
            "alice",
            session,
            provider,
            ctx,
            model,
            dispatcher: dispatcher,
            config: config);

        await PublishChannelMessageAsync(publisher, session, "go");
        await idle.WaitAsync(TimeSpan.FromSeconds(2));

        await Assert.That(model.PromptInvocations).IsEqualTo(2);
        await dispatcher.Received(1).DispatchAsync(
            Arg.Any<ToolCall>(),
            Arg.Any<ImmutableArray<ToolGroup>>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TurnLimitZeroKeepsDispatchingUntilTheModelStops()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventBus>();
        var session = new SessionId("alice", SessionId.DefaultSessionName);
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync(session, CancellationToken.None);
        var model = new ToolThenTextModel(toolRounds: 4);
        var dispatcher = Substitute.For<IToolCallDispatcher>();
        dispatcher
            .DispatchAsync(
                Arg.Any<ToolCall>(),
                Arg.Any<ImmutableArray<ToolGroup>>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ToolCallResult>(new ToolCallResult("ok", IsError: false)));
        using var idle = new IdleWaiter(publisher, session);

        await using var agent = await AgentHarness.StartAsync(
            "alice",
            session,
            provider,
            ctx,
            model,
            dispatcher: dispatcher);

        await PublishChannelMessageAsync(publisher, session, "go");
        await idle.WaitAsync(TimeSpan.FromSeconds(2));

        await Assert.That(model.PromptInvocations).IsEqualTo(5);
        await dispatcher.Received(4).DispatchAsync(
            Arg.Any<ToolCall>(),
            Arg.Any<ImmutableArray<ToolGroup>>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    private static ValueTask PublishChannelMessageAsync(
        IEventBus publisher,
        SessionId session,
        string text)
        => publisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = session },
            new ChannelMessage(text, TestChannelId, DateTimeOffset.UtcNow),
            CancellationToken.None);

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

    private sealed class IdleWaiter : IEventHandler<AgentLifecycleEvent>, IDisposable
    {
        private readonly TaskCompletionSource _idle =
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly IDisposable _subscription;

        public IdleWaiter(IEventBus bus, SessionId session)
        {
            _subscription = bus.Subscribe(
                Event.WellKnown.Agent.Idle with { Id = session },
                EventDeliveryMode.Awaited,
                this,
                preserveSubscriberExecutionContext: true);
        }

        public ValueTask HandleAsync(IEventEnvelope<AgentLifecycleEvent> envelope, CancellationToken cancellationToken)
        {
            _idle.TrySetResult();
            return ValueTask.CompletedTask;
        }

        public async Task WaitAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            try
            {
                await _idle.Task.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
            {
                throw new TimeoutException($"Agent did not idle within {timeout}.", ex);
            }
        }

        public void Dispose() => _subscription.Dispose();
    }

    private sealed class ToolThenTextModel : ILanguageModel
    {
        private readonly int _toolRounds;
        private int _invocations;

        public ToolThenTextModel(int toolRounds)
        {
            _toolRounds = toolRounds;
        }

        public int PromptInvocations => Volatile.Read(ref _invocations);

        public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
            ModelPrompt prompt,
            PromptOptions? options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var n = Interlocked.Increment(ref _invocations);
            cancellationToken.ThrowIfCancellationRequested();
            if (n <= _toolRounds)
            {
                yield return ScriptedLanguageModel.ToolCallFragment("test", "noop", "{}", n.ToString());
            }
            else
            {
                yield return new TextFragment("done");
            }

            await Task.Yield();
        }

        private sealed record TextFragment(string Content) : IModelTextResponse;
    }
}
