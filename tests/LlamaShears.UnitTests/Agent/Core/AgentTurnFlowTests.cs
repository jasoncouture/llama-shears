using System.Collections.Immutable;
using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Persistence;
using LlamaShears.Core.Sessions;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentTurnFlowTests
{
    private const string TestChannelId = "test";

    [Test]
    public async Task TwoChannelMessagesFromSameChannelProduceTwoSeparateUserTurnsInContext()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, "alice");
        var model = new ScriptedLanguageModel("ack");

        using var agent = BuildAgent("alice", provider, ctx, model);

        await PublishChannelMessageAsync(publisher, "alice", "first");
        await PublishChannelMessageAsync(publisher, "alice", "second");

        await captured.WaitForTurnAsync(TimeSpan.FromSeconds(5));

        var userTurns = ctx.Turns.Where(t => t.Role == ModelRole.User).ToArray();
        await Assert.That(userTurns).Count().IsEqualTo(2);
        await Assert.That(userTurns[0].Content).Contains("first");
        await Assert.That(userTurns[1].Content).Contains("second");
    }

    [Test]
    public async Task ChannelMessageProducesUserTurnWithChannelIdFromEnvelopeType()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, "alice");
        var model = new ScriptedLanguageModel("ack");

        using var agent = BuildAgent("alice", provider, ctx, model);

        await PublishChannelMessageAsync(publisher, "alice", "hello");
        await captured.WaitForTurnAsync(TimeSpan.FromSeconds(5));

        var userTurn = ctx.Turns.Single(t => t.Role == ModelRole.User);
        await Assert.That(userTurn.ChannelId).IsEqualTo(TestChannelId);
    }

    [Test]
    public async Task UserMessageArrivingDuringToolDispatchAppendsAfterToolTurns()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, "alice");

        var model = new TwoStageScriptedModel(
            stageOne: [ScriptedLanguageModel.ToolCallFragment("test", "noop", "{}", "1")],
            stageTwo: [TextResponseFragment("done")]);

        var dispatcher = new InlinePublishingDispatcher(publisher, "alice", "during-tool");

        using var agent = BuildAgent("alice", provider, ctx, model, dispatcher: dispatcher);

        await PublishChannelMessageAsync(publisher, "alice", "first");

        await WaitForAsync(() =>
            ctx.Turns.Count(t => t.Role == ModelRole.Assistant) >= 2,
            TimeSpan.FromSeconds(5));

        var roles = ctx.Turns.Select(t => t.Role).ToArray();
        var firstUser = Array.IndexOf(roles, ModelRole.User);
        var firstAssistant = Array.IndexOf(roles, ModelRole.Assistant);
        var firstTool = Array.IndexOf(roles, ModelRole.Tool);
        var secondUser = Array.IndexOf(roles, ModelRole.User, firstUser + 1);
        var secondAssistant = Array.IndexOf(roles, ModelRole.Assistant, firstAssistant + 1);

        await Assert.That(firstUser).IsGreaterThanOrEqualTo(0);
        await Assert.That(firstTool).IsGreaterThan(firstUser);
        await Assert.That(firstAssistant).IsGreaterThan(firstUser);
        await Assert.That(secondUser).IsGreaterThan(Math.Max(firstTool, firstAssistant));
        await Assert.That(secondAssistant).IsGreaterThan(secondUser);
    }

    private static IModelResponseFragment TextResponseFragment(string text)
        => new ScriptedTextFragment(text);

    private sealed record ScriptedTextFragment(string Content) : IModelTextResponse;

    private sealed class InlinePublishingDispatcher : IToolCallDispatcher
    {
        private readonly IEventPublisher _publisher;
        private readonly string _agentId;
        private readonly string _text;

        public InlinePublishingDispatcher(IEventPublisher publisher, string agentId, string text)
        {
            _publisher = publisher;
            _agentId = agentId;
            _text = text;
        }

        public async ValueTask<ToolCallResult> DispatchAsync(
            ToolCall call,
            ImmutableArray<ToolGroup> tools,
            string eventId,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            await _publisher.PublishAsync(
                Event.WellKnown.Channel.Message with { Id = TestChannelId },
                new ChannelMessage(_text, _agentId, DateTimeOffset.UtcNow),
                cancellationToken).ConfigureAwait(false);
            return new ToolCallResult("ok", IsError: false);
        }
    }

    private sealed class TwoStageScriptedModel : ILanguageModel
    {
        private readonly IReadOnlyList<IModelResponseFragment> _stageOne;
        private readonly IReadOnlyList<IModelResponseFragment> _stageTwo;
        private int _invocations;

        public TwoStageScriptedModel(
            IReadOnlyList<IModelResponseFragment> stageOne,
            IReadOnlyList<IModelResponseFragment> stageTwo)
        {
            _stageOne = stageOne;
            _stageTwo = stageTwo;
        }

        public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
            ModelPrompt prompt,
            PromptOptions? options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var stage = Interlocked.Increment(ref _invocations);
            var fragments = stage == 1 ? _stageOne : _stageTwo;
            foreach (var fragment in fragments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return fragment;
                await Task.Yield();
            }
        }
    }

    private static async Task WaitForAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
            {
                return;
            }
            await Task.Delay(20).ConfigureAwait(false);
        }
        throw new TimeoutException($"Predicate never became true within {timeout}.");
    }

    private static ValueTask PublishChannelMessageAsync(
        IEventPublisher publisher,
        string agentId,
        string text)
        => publisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = TestChannelId },
            new ChannelMessage(text, agentId, DateTimeOffset.UtcNow),
            CancellationToken.None);

    private static global::LlamaShears.Core.Agent BuildAgent(
        string id,
        IServiceProvider services,
        IAgentContext agentContext,
        ILanguageModel model,
        IToolCallDispatcher? dispatcher = null)
    {
        var compactor = Substitute.For<IContextCompactor>();
        compactor.CompactAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<ILanguageModel>(),
                Arg.Any<ModelConfiguration>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<ModelPrompt>()));
        var contextProvider = Substitute.For<IAgentContextProvider>();
        contextProvider.CreateAgentContextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(TestAgentConfigs.BuildAgentContext(id)));
        var publisher = services.GetRequiredService<IEventPublisher>();
        var currentAgent = new CurrentAgentAccessor();
        var resolvedConfig = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, id);
        var dataContextFactory = TestAgentConfigs.DataContextFactoryWith(resolvedConfig);
        var agent = new global::LlamaShears.Core.Agent(
            config: resolvedConfig,
            model: model,
            agentContext: agentContext,
            logger: NullLogger<global::LlamaShears.Core.Agent>.Instance,
            bus: services.GetRequiredService<IEventBus>(),
            systemPromptProvider: BuildStubSystemPromptProvider(),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            compactor: compactor,
            modelConfiguration: new ModelConfiguration(new ModelIdentity("test", "test")),
            agentContextProvider: contextProvider,
            eventPublisher: publisher,
            inferenceRunner: new InferenceRunner(
                publisher,
                dispatcher ?? Substitute.For<IToolCallDispatcher>(),
                TimeProvider.System,
                Substitute.For<IPromptContextProvider>(),
                TestAgentConfigs.EmptyMemorySearcher(),
                dataContextFactory),
            currentAgent: currentAgent,
            dataContextFactory: dataContextFactory,
            sessionFactory: services.GetRequiredService<ISessionFactory>(),
            scope: services.CreateAsyncScope());
            agent.Start();
            return agent;
    }

    private static ISystemPromptProvider BuildStubSystemPromptProvider()
    {
        var stub = Substitute.For<ISystemPromptProvider>();
        stub.GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("system"));
        return stub;
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
        services.AddSingleton<IContextStore>(new FakeContextStore());
        services.AddEventHandler<AgentTurnContextPersister>();
        services.AddSingleton<ISessionFactory, SessionFactory>();
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AgentTurnContextPersister>();
        return provider;
    }
}
