using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Sessions;
using LlamaShears.UnitTests.Agent.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Sessions;

public sealed class EphemeralSessionTests
{
    [Test]
    public async Task SessionReplyToolRouteEndsWithReplySentAndNoFallback()
    {
        var harness = new Harness();
        harness.IterationRunner.RunAsync(default!, default, default, default, default).ReturnsForAnyArgs(callInfo =>
        {
            harness.SessionContext.ReplySent = true;
            return Task.FromResult(new IterationOutcome(Interrupted: false, ToolResultTurns: []));
        });

        var session = harness.BuildSession();

        var result = await session.RunAsync("go", CancellationToken.None);

        await Assert.That(result.ReplySent).IsTrue();
        await Assert.That(result.UsedFallback).IsFalse();
        await Assert.That(result.Iterations).IsEqualTo(1);
    }

    [Test]
    public async Task NoReplyAndAssistantContentTurnTriggersFallbackPublish()
    {
        var harness = new Harness();
        var assistantTurn = new ModelTurn(
            ModelRole.Assistant,
            "hello back",
            DateTimeOffset.UnixEpoch)
        {
            SessionId = harness.SessionContext.SessionId,
        };
        await harness.AgentContext.AppendAsync(assistantTurn, CancellationToken.None);
        harness.IterationRunner.RunAsync(default!, default, default, default, default).ReturnsForAnyArgs(
            Task.FromResult(new IterationOutcome(Interrupted: false, ToolResultTurns: [])));

        var session = harness.BuildSession();

        var result = await session.RunAsync("go", CancellationToken.None);

        await Assert.That(result.ReplySent).IsTrue();
        await Assert.That(result.UsedFallback).IsTrue();
        await Assert.That(harness.Publisher.Channel.Count).IsEqualTo(1);
        var published = harness.Publisher.Channel[0];
        await Assert.That(published.Type.Id).IsEqualTo(harness.SessionContext.ChannelId);
        await Assert.That(published.Data!.AgentId).IsEqualTo("parent");
        await Assert.That(published.Data!.Text).IsEqualTo("hello back");
        await Assert.That(published.Data!.SessionId).IsEqualTo(harness.SessionContext.SessionId);
    }

    [Test]
    public async Task NoReplyAndNoAssistantContentReportsReplyNotSent()
    {
        var harness = new Harness();
        harness.IterationRunner.RunAsync(default!, default, default, default, default).ReturnsForAnyArgs(
            Task.FromResult(new IterationOutcome(Interrupted: false, ToolResultTurns: [])));

        var session = harness.BuildSession();

        var result = await session.RunAsync("go", CancellationToken.None);

        await Assert.That(result.ReplySent).IsFalse();
        await Assert.That(result.UsedFallback).IsFalse();
        await Assert.That(harness.Publisher.Channel.Count).IsEqualTo(0);
    }

    [Test]
    public async Task IterationCapHaltsLoopAfterMaxIterations()
    {
        var harness = new Harness(maxIterations: 3);
        harness.IterationRunner.RunAsync(default!, default, default, default, default).ReturnsForAnyArgs(callInfo =>
        {
            var toolTurn = new ModelTurn(ModelRole.Tool, "tool output", DateTimeOffset.UnixEpoch)
            {
                SessionId = harness.SessionContext.SessionId,
            };
            return Task.FromResult(new IterationOutcome(Interrupted: false, ToolResultTurns: [toolTurn]));
        });

        var session = harness.BuildSession();

        var result = await session.RunAsync("go", CancellationToken.None);

        await Assert.That(result.Iterations).IsEqualTo(3);
        await Assert.That(result.ReplySent).IsFalse();
    }

    [Test]
    public async Task InterruptedIterationStopsLoopAndSkipsFallback()
    {
        var harness = new Harness();
        harness.IterationRunner.RunAsync(default!, default, default, default, default).ReturnsForAnyArgs(
            Task.FromResult(new IterationOutcome(Interrupted: true, ToolResultTurns: [])));

        var session = harness.BuildSession();

        var result = await session.RunAsync("go", CancellationToken.None);

        await Assert.That(result.ReplySent).IsFalse();
        await Assert.That(harness.Publisher.Channel.Count).IsEqualTo(0);
    }

    private sealed class Harness
    {
        public Harness(int maxIterations = 8)
        {
            AgentContext = new FakeAgentContext("parent");
            IterationRunner = Substitute.For<IAgentIterationRunner>();
            Publisher = new RecordingPublisher();
            SessionContext = new EphemeralSessionContext
            {
                Parent = new EphemeralSessionReference("parent", null),
                SessionId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ChannelId = "ephemeral:test",
            };
            MaxIterations = maxIterations;
        }

        public IAgentContext AgentContext { get; }
        public IAgentIterationRunner IterationRunner { get; }
        public RecordingPublisher Publisher { get; }
        public EphemeralSessionContext SessionContext { get; }
        public int MaxIterations { get; }

        public EphemeralSession BuildSession() => new EphemeralSession(
            new NoopAsyncScope(),
            AgentContext,
            IterationRunner,
            Publisher,
            SessionContext,
            new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            NullLogger<EphemeralSession>.Instance,
            MaxIterations);
    }

    private sealed class NoopAsyncScope : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    internal sealed class RecordingPublisher : IEventPublisher
    {
        public List<RecordedEnvelope> Channel { get; } = [];

        public ValueTask PublishAsync<T>(EventType eventType, T? data, Guid correlationId, CancellationToken cancellationToken)
            where T : class
        {
            if (data is ChannelMessage cm)
            {
                Channel.Add(new RecordedEnvelope(eventType, cm, correlationId));
            }
            return ValueTask.CompletedTask;
        }
    }

    internal sealed record RecordedEnvelope(EventType Type, ChannelMessage Data, Guid CorrelationId);
}
