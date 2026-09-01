using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentIterationRunnerTests
{
    [Test]
    public async Task PrependsSystemPromptToTheModelPrompt()
    {
        var (runner, inference, agentContext) = BuildRunner();
        var system = new ModelTurn(ModelRole.System, "persona", DateTimeOffset.UnixEpoch);
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
            SystemPrompt = system,
        };
        ModelPrompt? sent = null;
        inference
            .RunAsync(Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<ModelPrompt>();
                return new InferenceOutcome("", "ok", null, [], []);
            });

        await runner.RunAsync(context);

        await Assert.That(sent).IsNotNull();
        await Assert.That(sent!.Turns[0]).IsEqualTo(system);
        await Assert.That(sent.Turns[1].Content).IsEqualTo("remembered");
    }

    [Test]
    public async Task OmitsSystemTurnWhenBagHasNone()
    {
        var (runner, inference, agentContext) = BuildRunner();
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
        };
        ModelPrompt? sent = null;
        inference
            .RunAsync(Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<ModelPrompt>();
                return new InferenceOutcome("", "ok", null, [], []);
            });

        await runner.RunAsync(context);

        await Assert.That(sent).IsNotNull();
        await Assert.That(sent!.Turns[0].Role).IsEqualTo(ModelRole.User);
        await Assert.That(sent.Turns[0].Content).IsEqualTo("remembered");
    }

    [Test]
    public async Task InsertsEphemeralBeforeTheLastUserTurn()
    {
        var (runner, inference, agentContext) = BuildRunner();
        var system = new ModelTurn(ModelRole.System, "persona", DateTimeOffset.UnixEpoch);
        var ephemeral = new ModelTurn(ModelRole.SystemEphemeral, "now", DateTimeOffset.UnixEpoch, Ephemeral: true);
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
            SystemPrompt = system,
            EphemeralContext = ephemeral,
        };
        ModelPrompt? sent = null;
        inference
            .RunAsync(Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<ModelPrompt>();
                return new InferenceOutcome("", "ok", null, [], []);
            });

        await runner.RunAsync(context);

        await Assert.That(sent).IsNotNull();
        await Assert.That(sent!.Turns[0]).IsEqualTo(system);
        await Assert.That(sent.Turns[1]).IsEqualTo(ephemeral);
        await Assert.That(sent.Turns[2].Content).IsEqualTo("remembered");
    }

    [Test]
    public async Task SkipsEphemeralWhenPromptDoesNotEndWithUser()
    {
        var (runner, inference, _) = BuildRunner();
        var ephemeral = new ModelTurn(ModelRole.SystemEphemeral, "now", DateTimeOffset.UnixEpoch, Ephemeral: true);
        IAgentContext agentContext = new FakeAgentContext(
            "alice",
            [new ModelTurn(ModelRole.Assistant, "done", DateTimeOffset.UnixEpoch)]);
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.Tool, "ok", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
            EphemeralContext = ephemeral,
        };
        ModelPrompt? sent = null;
        inference
            .RunAsync(Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<ModelPrompt>();
                return new InferenceOutcome("", "ok", null, [], []);
            });

        await runner.RunAsync(context);

        await Assert.That(sent).IsNotNull();
        await Assert.That(sent!.Turns.Select(turn => turn.Role).ToArray())
            .IsEquivalentTo([ModelRole.Assistant]);
    }

    private static (IAgentIterationRunner Runner, IInferenceRunner Inference, IAgentContext AgentContext) BuildRunner()
    {
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice");
        var session = new SessionId(config.Id, SessionId.DefaultSessionName);
        var dataScope = TestAgentConfigs.DataContextFactoryWith(config, session).Current!;
        var inference = Substitute.For<IInferenceRunner>();
        var compactor = Substitute.For<IContextCompactor>();
        compactor
            .CompactAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<ModelPrompt>()));
        var contextProvider = Substitute.For<IAgentContextProvider>();
        contextProvider
            .CreateAgentContextAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(TestAgentConfigs.BuildAgentContext(config.Id)));

        var services = new ServiceCollection();
        services.AddSingleton<IDataContextScope>(dataScope);
        services.AddSingleton(inference);
        services.AddSingleton(compactor);
        services.AddSingleton(TestAgentConfigs.BuildEmptyServerRegistry());
        services.AddSingleton(TestAgentConfigs.BuildEmptyToolDiscovery());
        services.AddSingleton<IAgentStateTracker>(new AgentStateTracker(dataScope));
        var provider = services.BuildServiceProvider();

        IAgentContext agentContext = new FakeAgentContext(
            config.Id,
            [new ModelTurn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch)]);
        IAgentIterationRunner runner = new AgentIterationRunner(
            NullLogger<AgentIterationRunner>.Instance,
            TimeProvider.System,
            Substitute.For<IEventBus>(),
            dataScope,
            provider.GetRequiredService<IServiceScopeFactory>(),
            contextProvider);
        return (runner, inference, agentContext);
    }
}
