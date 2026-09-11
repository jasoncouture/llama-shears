using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Eventing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class SubagentRunnerTests
{
    [Test]
    public async Task AwaitReturnsCapturedAssistantText()
    {
        using var harness = Harness.Create();
        harness.PublishChildTurnThenIdle("hello from child");

        var result = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work"),
            CancellationToken.None);

        await Assert.That(result.Ok).IsTrue();
        await Assert.That(result.Awaited).IsTrue();
        await Assert.That(result.Started).IsTrue();
        await Assert.That(result.TimedOut).IsFalse();
        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Output).IsEqualTo("hello from child");
        await Assert.That(result.SessionId).IsNotNull();
        await Assert.That(result.SessionId!).Contains("subagent-");
        await Assert.That(harness.LastStart!.Config.SystemPrompt).IsEqualTo("SUBAGENT.md");
        await Assert.That(harness.LastStart.Config.PromptContext).IsEqualTo("SUBAGENT.md");
        var policy = (TransientAgentReportPolicy)harness.LastStart.ContextData![TransientAgentReportPolicy.DataKey]!;
        await Assert.That(policy.ReportToParent).IsFalse();
    }

    [Test]
    public async Task FireAndForgetReturnsBeforeIdle()
    {
        using var harness = Harness.Create();
        var releaseIdle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Spawner
            .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var info = call.Arg<PromptedAgentStartInformation>();
                harness.LastStart = info;
                _ = Task.Run(async () =>
                {
                    await releaseIdle.Task;
                    await harness.PublishIdleAsync(info);
                });
                return new ValueTask<AgentHandle>(Harness.HandleFor(info));
            });

        var result = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work", AwaitResult: false),
            CancellationToken.None);
        releaseIdle.TrySetResult();

        await Assert.That(result.Started).IsTrue();
        await Assert.That(result.Awaited).IsFalse();
        await Assert.That(result.Ok).IsTrue();
        await Assert.That(result.Error).IsNull();
        await Assert.That(result.TimedOut).IsFalse();
        await Assert.That(result.Output).IsNull();
        var policy = (TransientAgentReportPolicy)harness.LastStart!.ContextData![TransientAgentReportPolicy.DataKey]!;
        await Assert.That(policy.ReportToParent).IsTrue();
    }

    [Test]
    public async Task TimeoutSetsTimedOutAndPublishesStop()
    {
        using var harness = Harness.Create();
        AgentStopRequest? stop = null;
        using var stopSub = harness.Bus.Subscribe<AgentStopRequest>(
            $"{Event.WellKnown.Command.AgentStop}:+",
            EventDeliveryMode.Awaited,
            (envelope, _) =>
            {
                stop = envelope.Data;
                return ValueTask.CompletedTask;
            });
        harness.Spawner
            .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var info = call.Arg<PromptedAgentStartInformation>();
                harness.LastStart = info;
                return ValueTask.FromResult(Harness.HandleFor(info));
            });

        var result = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work", TimeoutSeconds: 1),
            CancellationToken.None);

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.TimedOut).IsTrue();
        await Assert.That(result.Started).IsTrue();
        await Assert.That(result.Awaited).IsTrue();
        await Assert.That(result.Error).Contains("Timed out");
        await Assert.That(stop).IsNotNull();
        await Assert.That(stop!.SessionId).IsEqualTo(harness.LastStart!.Id);
    }

    [Test]
    public async Task TimeoutKeepsPartialAssistantText()
    {
        using var harness = Harness.Create();
        harness.Spawner
            .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>())
            .Returns(call => harness.PublishTurnThenReturnAsync(call, "partial"));

        var result = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work", TimeoutSeconds: 1),
            CancellationToken.None);

        await Assert.That(result.TimedOut).IsTrue();
        await Assert.That(result.Output).IsEqualTo("partial");
    }

    [Test]
    public async Task NestedCallerIsRefused()
    {
        using var harness = Harness.Create(nested: true);

        var result = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work"),
            CancellationToken.None);

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.Error).Contains("nested");
        await harness.Spawner.DidNotReceive()
            .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MissingAgentIsRefused()
    {
        using var harness = Harness.Create(includeConfig: false);

        var result = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work"),
            CancellationToken.None);

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.Error).Contains("authenticated agent");
        await harness.Spawner.DidNotReceive()
            .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BadModelStringIsRefused()
    {
        using var harness = Harness.Create();

        var result = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work", Model: "not-a-model"),
            CancellationToken.None);

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.Error).Contains("not a valid model identity");
        await harness.Spawner.DidNotReceive()
            .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ModelOverlayUsesParsedIdentity()
    {
        using var harness = Harness.Create();
        harness.PublishChildTurnThenIdle("ok");

        await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work", Model: "ollama/llama3"),
            CancellationToken.None);

        await Assert.That(harness.LastStart!.Config.Model.Id)
            .IsEqualTo(new CompositeIdentity("ollama", "llama3"));
    }

    [Test]
    public async Task ContextIsPrependedToThePrompt()
    {
        using var harness = Harness.Create();
        harness.PublishChildTurnThenIdle("ok");

        await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work", Context: "background notes"),
            CancellationToken.None);

        await Assert.That(harness.LastStart!.InitialPrompt.Content)
            .IsEqualTo("background notes\n\ndo the work");
    }

    [Test]
    public async Task TimeoutOutOfRangeIsRefused()
    {
        using var harness = Harness.Create();

        var zero = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work", TimeoutSeconds: 0),
            CancellationToken.None);
        var high = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work", TimeoutSeconds: 601),
            CancellationToken.None);

        await Assert.That(zero.Error).Contains("timeoutSeconds");
        await Assert.That(high.Error).Contains("timeoutSeconds");
        await harness.Spawner.DidNotReceive()
            .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MaxTurnsOutOfRangeIsRefused()
    {
        using var harness = Harness.Create();

        var zero = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work", MaxTurns: 0),
            CancellationToken.None);
        var high = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work", MaxTurns: 65),
            CancellationToken.None);

        await Assert.That(zero.Error).Contains("maxTurns");
        await Assert.That(high.Error).Contains("maxTurns");
        await harness.Spawner.DidNotReceive()
            .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MaxTurnsOverlaysChildTurnLimit()
    {
        using var harness = Harness.Create();
        harness.PublishChildTurnThenIdle("ok");

        await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work", MaxTurns: 3),
            CancellationToken.None);

        await Assert.That(harness.LastStart!.Config.Tools.TurnLimit).IsEqualTo(3);
    }

    [Test]
    public async Task OmittedMaxTurnsKeepsParentToolBudget()
    {
        using var harness = Harness.Create();
        harness.PublishChildTurnThenIdle("ok");

        await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work"),
            CancellationToken.None);

        await Assert.That(harness.LastStart!.Config.Tools.TurnLimit).IsEqualTo(0);
    }

    [Test]
    public async Task RemintedChildSessionIdIsRefusedAndStopped()
    {
        using var harness = Harness.Create();
        AgentStopRequest? stop = null;
        using var stopSub = harness.Bus.Subscribe<AgentStopRequest>(
            $"{Event.WellKnown.Command.AgentStop}:+",
            EventDeliveryMode.Awaited,
            (envelope, _) =>
            {
                stop = envelope.Data;
                return ValueTask.CompletedTask;
            });
        harness.Spawner
            .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var info = call.Arg<PromptedAgentStartInformation>();
                harness.LastStart = info;
                var reminted = SessionId.CreateFor(info.Id.AgentId, info.Id.Name);
                var path = info.ParentSessionPath.CreateChildSession(reminted);
                var serviceScope = new AsyncServiceScope(Substitute.For<IServiceScope>());
                return new ValueTask<AgentHandle>(
                    new AgentHandle(path, "hash", serviceScope, ExecutionContext.Capture()!, typeof(IAgent)));
            });

        var result = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work"),
            CancellationToken.None);

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.Started).IsFalse();
        await Assert.That(result.Error).Contains("does not match");
        await Assert.That(stop).IsNotNull();
        await Assert.That(stop!.SessionId).IsNotEqualTo(harness.LastStart!.Id);
    }

    [Test]
    public async Task SpawnFailureReturnsError()
    {
        using var harness = Harness.Create();
        harness.Spawner
            .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await harness.Runner.RunAsync(
            new SubagentRunRequest("do the work"),
            CancellationToken.None);

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.Started).IsFalse();
        await Assert.That(result.Error).Contains("boom");
    }

    [Test]
    public async Task EmptyPromptIsRefused()
    {
        using var harness = Harness.Create();

        var result = await harness.Runner.RunAsync(
            new SubagentRunRequest("   "),
            CancellationToken.None);

        await Assert.That(result.Error).Contains("prompt is required");
        await harness.Spawner.DidNotReceive()
            .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>());
    }

    private sealed class Harness : IDisposable
    {
        private readonly ServiceProvider _provider;

        private Harness(
            ServiceProvider provider,
            IEventBus bus,
            IPromptedAgentSpawner spawner,
            ISubagentRunner runner)
        {
            _provider = provider;
            Bus = bus;
            Spawner = spawner;
            Runner = runner;
        }

        public IEventBus Bus { get; }
        public IPromptedAgentSpawner Spawner { get; }
        public ISubagentRunner Runner { get; }
        public PromptedAgentStartInformation? LastStart { get; set; }

        public static Harness Create(bool nested = false, bool includeConfig = true)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEventingFramework();
            var provider = services.BuildServiceProvider();
            var bus = provider.GetRequiredService<IEventBus>();
            var spawner = Substitute.For<IPromptedAgentSpawner>();
            var root = SessionId.CreateFor("alice");
            IDataContextScope scope = new FakeDataContextScope(root);
            if (includeConfig)
            {
                var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice");
                scope.SetItem(AgentConfig.DataKey, config);
            }

            var path = nested
                ? new SessionPath(root).CreateChildSession(SessionId.CreateFor("alice", "heartbeat"))
                : new SessionPath(root);
            scope.SetItem(SessionPath.DataKey, path);

            ISubagentRunner runner = new SubagentRunner(
                scope,
                spawner,
                bus,
                new FakeTimeProvider(DateTimeOffset.UnixEpoch));
            return new Harness(provider, bus, spawner, runner);
        }

        public void PublishChildTurnThenIdle(string text)
        {
            Spawner
                .CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>())
                .Returns(call => PublishTurnThenIdleAsync(call, text));
        }

        public async ValueTask<AgentHandle> PublishTurnThenIdleAsync(NSubstitute.Core.CallInfo call, string text)
        {
            var info = call.Arg<PromptedAgentStartInformation>();
            LastStart = info;
            await PublishTurnAsync(info, text);
            await PublishIdleAsync(info);
            return HandleFor(info);
        }

        public async ValueTask<AgentHandle> PublishTurnThenReturnAsync(NSubstitute.Core.CallInfo call, string text)
        {
            var info = call.Arg<PromptedAgentStartInformation>();
            LastStart = info;
            await PublishTurnAsync(info, text);
            return HandleFor(info);
        }

        public ValueTask PublishTurnAsync(PromptedAgentStartInformation info, string text)
            => Bus.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = info.Id },
                new ModelTurn(ModelRole.Assistant, text, DateTimeOffset.UnixEpoch),
                CancellationToken.None);

        public ValueTask PublishIdleAsync(PromptedAgentStartInformation info)
            => Bus.PublishAsync(
                Event.WellKnown.Agent.Idle with { Id = info.Id },
                new AgentLifecycleEvent(info.Config, info.Id),
                CancellationToken.None);

        public static AgentHandle HandleFor(PromptedAgentStartInformation info)
        {
            var path = info.ParentSessionPath.CreateChildSession(info.Id);
            var serviceScope = new AsyncServiceScope(Substitute.For<IServiceScope>());
            return new AgentHandle(path, "hash", serviceScope, ExecutionContext.Capture()!, typeof(IAgent));
        }

        public void Dispose() => _provider.Dispose();
    }
}
