using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentIterationRunnerTests
{
    [Test]
    public async Task SendsTheBagPromptToInference()
    {
        var (runner, inference, agentContext) = BuildRunner();
        var prompt = new ModelPrompt(
            [new ModelTurn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch)]);
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
            Prompt = prompt,
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

        await Assert.That(sent).IsEqualTo(prompt);
    }

    [Test]
    public async Task InsertsEphemeralBeforeTheLastUserTurn()
    {
        var (runner, inference, agentContext) = BuildRunner();
        var system = new ModelTurn(ModelRole.System, "persona", DateTimeOffset.UnixEpoch);
        var remembered = new ModelTurn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch);
        var ephemeral = new ModelTurn(ModelRole.SystemEphemeral, "now", DateTimeOffset.UnixEpoch, Ephemeral: true);
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
            EphemeralContext = ephemeral,
            Prompt = new ModelPrompt([system, remembered]),
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
        await Assert.That(sent.Turns[2]).IsEqualTo(remembered);
    }

    [Test]
    public async Task SkipsEphemeralWhenPromptDoesNotEndWithUser()
    {
        var (runner, inference, _) = BuildRunner();
        var assistant = new ModelTurn(ModelRole.Assistant, "done", DateTimeOffset.UnixEpoch);
        var ephemeral = new ModelTurn(ModelRole.SystemEphemeral, "now", DateTimeOffset.UnixEpoch, Ephemeral: true);
        IAgentContext agentContext = new FakeAgentContext("alice", [assistant]);
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.Tool, "ok", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
            EphemeralContext = ephemeral,
            Prompt = new ModelPrompt([assistant]),
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

    [Test]
    public async Task InsertsEphemeralOnceAcrossEmptyResponseRetries()
    {
        var (runner, inference, agentContext) = BuildRunner();
        var remembered = new ModelTurn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch);
        var ephemeral = new ModelTurn(ModelRole.SystemEphemeral, "now", DateTimeOffset.UnixEpoch, Ephemeral: true);
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
            EphemeralContext = ephemeral,
            Prompt = new ModelPrompt([remembered]),
        };
        var sent = new List<ModelPrompt>();
        inference
            .RunAsync(Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent.Add(call.Arg<ModelPrompt>());
                return sent.Count == 1
                    ? new InferenceOutcome("", "", null, [], [])
                    : new InferenceOutcome("", "ok", null, [], []);
            });

        await runner.RunAsync(context);

        await Assert.That(sent.Count).IsEqualTo(2);
        await Assert.That(sent[0].Turns.Count(turn => turn.Ephemeral)).IsEqualTo(1);
        await Assert.That(sent[1].Turns.Count(turn => turn.Ephemeral)).IsEqualTo(1);
        await Assert.That(sent[1].Turns.Count).IsEqualTo(sent[0].Turns.Count + 1);
        await Assert.That(sent[1].Turns[^1].Role).IsEqualTo(ModelRole.User);
    }

    [Test]
    public async Task ThrowsWhenTheBagHasNoPrompt()
    {
        var (runner, _, agentContext) = BuildRunner();
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
        };

        await Assert.That(async () => await runner.RunAsync(context))
            .Throws<InvalidOperationException>();
    }

    private static (IAgentIterationRunner Runner, IInferenceRunner Inference, IAgentContext AgentContext) BuildRunner()
    {
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice");
        var session = new SessionId(config.Id, SessionId.DefaultSessionName);
        var dataScope = TestAgentConfigs.DataContextFactoryWith(config, session).Current!;
        var inference = Substitute.For<IInferenceRunner>();

        var services = new ServiceCollection();
        services.AddSingleton<IDataContextScope>(dataScope);
        services.AddSingleton(inference);
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
            dataScope,
            provider.GetRequiredService<IServiceScopeFactory>());
        return (runner, inference, agentContext);
    }
}
