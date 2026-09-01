using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Pipeline;
using LlamaShears.UnitTests.Agent.Core;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class EphemeralContextMiddlewareTests
{
    [Test]
    public async Task RendersThePromptContextOntoTheBag()
    {
        var provider = Substitute.For<IPromptContextProvider>();
        provider
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("<system>now</system>"));
        var searcher = Substitute.For<IMemorySearcher>();
        searcher
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<double?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyList<MemorySearchResult>>([]));
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var scope = PipelineTestContext.ScopeFor();
        IAgentMiddleware middleware = new EphemeralContextMiddleware(
            provider,
            searcher,
            new AgentStateTracker(scope),
            scope,
            time);
        var remembered = new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch);
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [remembered]),
            [new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            Prompt = new ModelPrompt([remembered]),
        };
        var nextCalled = false;

        await middleware.InvokeAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(context.EphemeralContext).IsNotNull();
        await Assert.That(context.EphemeralContext!.Role).IsEqualTo(ModelRole.SystemEphemeral);
        await Assert.That(context.EphemeralContext.Content).IsEqualTo("<system>now</system>");
        await Assert.That(context.EphemeralContext.Ephemeral).IsTrue();
        await Assert.That(context.EphemeralContext.Timestamp).IsEqualTo(DateTimeOffset.UnixEpoch);
        await Assert.That(context.Prompt).IsNotNull();
        await Assert.That(context.Prompt!.Turns[0]).IsEqualTo(remembered);
        await Assert.That(context.Prompt.Turns[1]).IsEqualTo(context.EphemeralContext);
        await searcher.Received().SearchAsync(
            "alice",
            "hello",
            null,
            null,
            context.TurnToken);
        await provider.Received(1).GetAsync(
            null,
            Arg.Is<IReadOnlyDictionary<string, object?>>(data => data.ContainsKey("memories")),
            context.TurnToken);
    }

    [Test]
    public async Task StampsAgentStateBeforeRenderingTheTemplate()
    {
        IReadOnlyDictionary<string, object?>? snapshot = null;
        var provider = Substitute.For<IPromptContextProvider>();
        provider
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                snapshot = call.Arg<IReadOnlyDictionary<string, object?>>();
                return ValueTask.FromResult<string?>("ok");
            });
        var scope = PipelineTestContext.ScopeFor();
        var correlation = Guid.CreateVersion7();
        var session = Guid.CreateVersion7();
        IAgentMiddleware middleware = new EphemeralContextMiddleware(
            provider,
            TestAgentConfigs.EmptyMemorySearcher(),
            new AgentStateTracker(scope),
            scope,
            TimeProvider.System);
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice"),
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch, ChannelId: "telegram:1")
            {
                SessionId = session,
            }],
            CancellationToken.None)
        {
            CorrelationId = correlation,
            Prompt = new ModelPrompt([new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)]),
        };

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await Assert.That(snapshot).IsNotNull();
        await Assert.That(snapshot!.ContainsKey(AgentState.DataKey)).IsTrue();
        var state = (AgentState)snapshot[AgentState.DataKey]!;
        await Assert.That(state.ChannelId).IsEqualTo("telegram:1");
        await Assert.That(state.CorrelationId).IsEqualTo(correlation);
        await Assert.That(state.SessionId).IsEqualTo(session);
    }

    [Test]
    public async Task LeavesTheBagEmptyWhenTheTemplateIsBlank()
    {
        var provider = Substitute.For<IPromptContextProvider>();
        provider
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("   "));
        var scope = PipelineTestContext.ScopeFor();
        IAgentMiddleware middleware = new EphemeralContextMiddleware(
            provider,
            TestAgentConfigs.EmptyMemorySearcher(),
            new AgentStateTracker(scope),
            scope,
            TimeProvider.System);
        var context = PipelineTestContext.Create();

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await Assert.That(context.EphemeralContext).IsNull();
    }

    [Test]
    public async Task UsesTurnTokenForProviderAndSearch()
    {
        var provider = Substitute.For<IPromptContextProvider>();
        provider
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("block"));
        var searcher = Substitute.For<IMemorySearcher>();
        searcher
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<double?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyList<MemorySearchResult>>([]));
        var scope = PipelineTestContext.ScopeFor();
        IAgentMiddleware middleware = new EphemeralContextMiddleware(
            provider,
            searcher,
            new AgentStateTracker(scope),
            scope,
            TimeProvider.System);
        using var turn = new CancellationTokenSource();
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice"),
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            TurnToken = turn.Token,
            Prompt = new ModelPrompt([new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)]),
        };

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await searcher.Received().SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int?>(),
            Arg.Any<double?>(),
            turn.Token);
        await provider.Received(1).GetAsync(
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            turn.Token);
    }

    [Test]
    public async Task SearchesEachQueryOnceWhenTurnsAlreadyIncludeTheBatch()
    {
        var searcher = Substitute.For<IMemorySearcher>();
        searcher
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<double?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyList<MemorySearchResult>>([]));
        var provider = Substitute.For<IPromptContextProvider>();
        provider
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("now"));
        var scope = PipelineTestContext.ScopeFor();
        IAgentMiddleware middleware = new EphemeralContextMiddleware(
            provider,
            searcher,
            new AgentStateTracker(scope),
            scope,
            TimeProvider.System);
        var user = new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UnixEpoch);
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [user]),
            [user],
            CancellationToken.None)
        {
            Prompt = new ModelPrompt([user]),
        };

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await searcher.Received(1).SearchAsync(
            "alice",
            "hello",
            null,
            null,
            context.TurnToken);
    }

    [Test]
    public async Task InsertsIntoTheBagPromptBeforeTheLastUserTurn()
    {
        var provider = Substitute.For<IPromptContextProvider>();
        provider
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("now"));
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var scope = PipelineTestContext.ScopeFor();
        IAgentMiddleware middleware = new EphemeralContextMiddleware(
            provider,
            TestAgentConfigs.EmptyMemorySearcher(),
            new AgentStateTracker(scope),
            scope,
            time);
        var system = new ModelTurn(ModelRole.System, "persona", DateTimeOffset.UnixEpoch);
        var remembered = new ModelTurn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch);
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [remembered]),
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            Prompt = new ModelPrompt([system, remembered]),
        };

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await Assert.That(context.Prompt).IsNotNull();
        await Assert.That(context.Prompt!.Turns[0]).IsEqualTo(system);
        await Assert.That(context.Prompt.Turns[1]).IsEqualTo(context.EphemeralContext);
        await Assert.That(context.Prompt.Turns[2]).IsEqualTo(remembered);
    }

    [Test]
    public async Task SkipsInsertWhenPromptDoesNotEndWithUser()
    {
        var provider = Substitute.For<IPromptContextProvider>();
        provider
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("now"));
        var scope = PipelineTestContext.ScopeFor();
        IAgentMiddleware middleware = new EphemeralContextMiddleware(
            provider,
            TestAgentConfigs.EmptyMemorySearcher(),
            new AgentStateTracker(scope),
            scope,
            TimeProvider.System);
        var assistant = new ModelTurn(ModelRole.Assistant, "done", DateTimeOffset.UnixEpoch);
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [assistant]),
            [new ModelTurn(ModelRole.Tool, "ok", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            Prompt = new ModelPrompt([assistant]),
        };

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await Assert.That(context.EphemeralContext).IsNotNull();
        await Assert.That(context.Prompt!.Turns.Select(turn => turn.Role).ToArray())
            .IsEquivalentTo([ModelRole.Assistant]);
    }

    [Test]
    public async Task ThrowsWhenTheTemplateRenderedAndTheBagHasNoPrompt()
    {
        var provider = Substitute.For<IPromptContextProvider>();
        provider
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("now"));
        var scope = PipelineTestContext.ScopeFor();
        IAgentMiddleware middleware = new EphemeralContextMiddleware(
            provider,
            TestAgentConfigs.EmptyMemorySearcher(),
            new AgentStateTracker(scope),
            scope,
            TimeProvider.System);
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice"),
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None);

        await Assert.That(async () => await middleware.InvokeAsync(
                context,
                (_, _) => Task.CompletedTask,
                CancellationToken.None))
            .Throws<InvalidOperationException>();
    }
}
