using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using LlamaShears.Core.Persistence;
using LlamaShears.Core.Sessions;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentInterruptGracefulTests
{
    private const string TestChannelId = "test";

    [Test]
    public async Task InterruptDuringTextStreamPersistsPartialAssistantTurn()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        var model = new GatedTextStreamModel("partial-content");
        using var agent = BuildAgent("alice", provider, ctx, model);

        await PublishChannelMessageAsync(publisher, "alice", "go");
        await model.WaitForFragmentEmittedAsync(TimeSpan.FromSeconds(5));

        await agent.InterruptAsync(CancellationToken.None);

        using var lockTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await agent.LockAsync(lockTimeout.Token);
        await agent.UnlockAsync();

        var assistantTurns = ctx.Turns.Where(t => t.Role == ModelRole.Assistant).ToArray();
        await Assert.That(assistantTurns).Count().IsEqualTo(1);
        await Assert.That(assistantTurns[0].Content).IsEqualTo("partial-content");
    }

    [Test]
    public async Task InterruptDuringToolDispatchPairsToolCallWithErrorResult()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        var model = new GatedToolCallModel(
            new ToolCall("test", "noop", "{}", "1"));
        var dispatcher = new HangingDispatcher(publisher);

        using var agent = BuildAgent("alice", provider, ctx, model, dispatcher: dispatcher);

        await PublishChannelMessageAsync(publisher, "alice", "go");
        await dispatcher.WaitForDispatchAsync(TimeSpan.FromSeconds(5));

        await agent.InterruptAsync(CancellationToken.None);

        using var lockTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await agent.LockAsync(lockTimeout.Token);
        await agent.UnlockAsync();

        var assistantTurns = ctx.Turns.Where(t => t.Role == ModelRole.Assistant).ToArray();
        await Assert.That(assistantTurns).Count().IsEqualTo(1);
        await Assert.That(assistantTurns[0].ToolCalls.Length).IsEqualTo(1);
        await Assert.That(assistantTurns[0].ToolCalls[0].CallId).IsEqualTo("1");

        var toolTurns = ctx.Turns.Where(t => t.Role == ModelRole.Tool).ToArray();
        await Assert.That(toolTurns).Count().IsEqualTo(1);
        await Assert.That(toolTurns[0].IsError).IsTrue();
        await Assert.That(toolTurns[0].Content).Contains("interrupted");
    }

    private sealed class GatedTextStreamModel : ILanguageModel
    {
        private readonly string _text;
        private readonly TaskCompletionSource _emitted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public GatedTextStreamModel(string text)
        {
            _text = text;
        }

        public Task WaitForFragmentEmittedAsync(TimeSpan timeout) => _emitted.Task.WaitAsync(timeout);

        public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
            ModelPrompt prompt,
            PromptOptions? options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new TextFragment(_text);
            _emitted.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            yield break;
        }
    }

    private sealed class GatedToolCallModel : ILanguageModel
    {
        private readonly ToolCall _call;

        public GatedToolCallModel(ToolCall call)
        {
            _call = call;
        }

        public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
            ModelPrompt prompt,
            PromptOptions? options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new ToolCallFragmentImpl(_call);
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            yield break;
        }
    }

    private sealed class HangingDispatcher : IToolCallDispatcher
    {
        private readonly TaskCompletionSource _dispatched = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly IEventPublisher _publisher;

        public HangingDispatcher(IEventPublisher publisher)
        {
            _publisher = publisher;
        }

        public Task WaitForDispatchAsync(TimeSpan timeout) => _dispatched.Task.WaitAsync(timeout);

        public async ValueTask<ToolCallResult> DispatchAsync(
            ToolCall call,
            ImmutableArray<ToolGroup> tools,
            string eventId,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            _dispatched.TrySetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            var result = new ToolCallResult($"Tool '{call.Name}' on server '{call.Source}' was interrupted by user.", IsError: true);
            await _publisher.PublishAsync(
                Event.WellKnown.Agent.ToolResult with { Id = eventId },
                new AgentToolResultFragment(call.Source, call.Name, result.Content, result.IsError, call.CallId),
                correlationId,
                CancellationToken.None).ConfigureAwait(false);
            return result;
        }
    }

    private sealed record TextFragment(string Content) : IModelTextResponse;

    private sealed record ToolCallFragmentImpl(ToolCall Call) : IModelToolCallFragment;

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
        var agent = new global::LlamaShears.Core.Agent(
            config: TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, id),
            model: model,
            agentContext: agentContext,
            loggerFactory: NullLoggerFactory.Instance,
            bus: services.GetRequiredService<IEventBus>(),
            systemPromptProvider: BuildStubSystemPromptProvider(),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            compactor: compactor,
            modelConfiguration: new ModelConfiguration("test"),
            agentContextProvider: contextProvider,
            eventPublisher: publisher,
            inferenceRunner: new InferenceRunner(
                publisher,
                dispatcher ?? Substitute.For<IToolCallDispatcher>(),
                TimeProvider.System,
                Substitute.For<IPromptContextProvider>(),
                TestAgentConfigs.EmptyMemorySearcher(),
                Substitute.For<IAgentConfigProvider>(),
                currentAgent),
            currentAgent: currentAgent,
            dataContextFactory: Substitute.For<IDataContextFactory>(),
            sessionFactory: services.GetRequiredService<ISessionFactory>(),
            scope: services.CreateAsyncScope());
            agent.Start();
            return agent;
    }

    private static ISystemPromptProvider BuildStubSystemPromptProvider()
    {
        var stub = Substitute.For<ISystemPromptProvider>();
        stub.GetAsync(Arg.Any<string?>(), Arg.Any<SystemPromptTemplateParameters>(), Arg.Any<CancellationToken>())
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
