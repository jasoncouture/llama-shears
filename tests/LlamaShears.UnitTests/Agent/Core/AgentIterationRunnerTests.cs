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
    public async Task SendsTheBagPromptSessionCorrelationAndChannelToInference()
    {
        var (runner, inference, agentContext, session) = BuildRunner();
        var prompt = new ModelPrompt(
            [new ModelTurn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch)]);
        var correlation = Guid.CreateVersion7();
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch, ChannelId: "telegram:1")],
            CancellationToken.None)
        {
            CorrelationId = correlation,
            Prompt = prompt,
            SessionId = session,
            ChannelId = "telegram:1",
        };
        ModelPrompt? sent = null;
        inference
            .RunAsync(
                Arg.Any<ModelPrompt>(),
                Arg.Any<PromptOptions?>(),
                Arg.Any<SessionId>(),
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<ModelPrompt>();
                return new InferenceOutcome("", "ok", null, []);
            });

        await runner.RunAsync(context);

        await Assert.That(sent).IsEqualTo(prompt);
        await inference.Received(1).RunAsync(
            prompt,
            Arg.Any<PromptOptions?>(),
            session,
            correlation,
            "telegram:1",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReturnsToolCallsWithoutDispatchingThem()
    {
        var (runner, inference, agentContext, session) = BuildRunner();
        var call = new ToolCall("llamashears", "file_read", "{}", "1");
        inference
            .RunAsync(
                Arg.Any<ModelPrompt>(),
                Arg.Any<PromptOptions?>(),
                Arg.Any<SessionId>(),
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new InferenceOutcome("", "", null, [call]));
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
            Prompt = new ModelPrompt([new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)]),
            SessionId = session,
        };

        var outcome = await runner.RunAsync(context);

        await Assert.That(outcome.ToolCalls.Length).IsEqualTo(1);
        await Assert.That(outcome.ToolCalls[0]).IsEqualTo(call);
        await Assert.That(outcome.ToolResultTurns.IsDefaultOrEmpty).IsTrue();
    }

    [Test]
    public async Task AppendsEmptyResponseRetryWithoutChangingExistingTurns()
    {
        var (runner, inference, agentContext, session) = BuildRunner();
        var remembered = new ModelTurn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch);
        var ephemeral = new ModelTurn(ModelRole.SystemEphemeral, "now", DateTimeOffset.UnixEpoch, Ephemeral: true);
        var prompt = new ModelPrompt([ephemeral, remembered]);
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
            Prompt = prompt,
            SessionId = session,
        };
        var sent = new List<ModelPrompt>();
        inference
            .RunAsync(
                Arg.Any<ModelPrompt>(),
                Arg.Any<PromptOptions?>(),
                Arg.Any<SessionId>(),
                Arg.Any<Guid>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent.Add(call.Arg<ModelPrompt>());
                return sent.Count == 1
                    ? new InferenceOutcome("", "", null, [])
                    : new InferenceOutcome("", "ok", null, []);
            });

        await runner.RunAsync(context);

        await Assert.That(sent.Count).IsEqualTo(2);
        await Assert.That(sent[0]).IsEqualTo(prompt);
        await Assert.That(sent[1].Turns.Count(turn => turn.Ephemeral)).IsEqualTo(1);
        await Assert.That(sent[1].Turns.Count).IsEqualTo(prompt.Turns.Count + 1);
        await Assert.That(sent[1].Turns[^1].Role).IsEqualTo(ModelRole.User);
    }

    [Test]
    public async Task ThrowsWhenTheBagHasNoPrompt()
    {
        var (runner, _, agentContext, session) = BuildRunner();
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
            SessionId = session,
        };

        await Assert.That(async () => await runner.RunAsync(context))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ThrowsWhenTheBagHasNoSessionId()
    {
        var (runner, _, agentContext, _) = BuildRunner();
        var context = new AgentPipelineContext(
            agentContext,
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
            Prompt = new ModelPrompt(
                [new ModelTurn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch)]),
        };

        await Assert.That(async () => await runner.RunAsync(context))
            .Throws<InvalidOperationException>();
    }

    private static (IAgentIterationRunner Runner, IInferenceRunner Inference, IAgentContext AgentContext, SessionId Session) BuildRunner()
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
        return (runner, inference, agentContext, session);
    }
}
