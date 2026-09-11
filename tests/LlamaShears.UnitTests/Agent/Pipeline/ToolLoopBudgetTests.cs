using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Pipeline;
using LlamaShears.UnitTests.Agent.Core;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class ToolLoopBudgetTests
{
    [Test]
    public async Task UserBatchStartsAtIterationOne()
    {
        var budget = Budget(turnLimit: 0);

        budget.ObserveBatch([User("hi")]);

        await Assert.That(budget.Iteration).IsEqualTo(1);
        await Assert.That(budget.IsFinal).IsFalse();
    }

    [Test]
    public async Task ToolOnlyBatchContinuesTheSameWindow()
    {
        var budget = Budget(turnLimit: 0);
        budget.ObserveBatch([User("hi")]);

        budget.ObserveBatch([Tool("ok")]);

        await Assert.That(budget.Iteration).IsEqualTo(2);
    }

    [Test]
    public async Task NextUserBatchResetsTheWindow()
    {
        var budget = Budget(turnLimit: 0);
        budget.ObserveBatch([User("hi")]);
        budget.ObserveBatch([Tool("ok")]);

        budget.ObserveBatch([User("again")]);

        await Assert.That(budget.Iteration).IsEqualTo(1);
    }

    [Test]
    public async Task FrameworkUserBatchResetsTheWindow()
    {
        var budget = Budget(turnLimit: 0);
        budget.ObserveBatch([User("hi")]);
        budget.ObserveBatch([Tool("ok")]);

        budget.ObserveBatch([new ModelTurn(ModelRole.FrameworkUser, "tick", DateTimeOffset.UnixEpoch)]);

        await Assert.That(budget.Iteration).IsEqualTo(1);
    }

    [Test]
    public async Task PositiveLimitMarksTheNthIterationFinal()
    {
        var budget = Budget(turnLimit: 2);
        budget.ObserveBatch([User("hi")]);
        await Assert.That(budget.IsFinal).IsFalse();

        budget.ObserveBatch([Tool("ok")]);

        await Assert.That(budget.Iteration).IsEqualTo(2);
        await Assert.That(budget.IsFinal).IsTrue();
    }

    [Test]
    public async Task LimitOfOneIsFinalOnTheFirstRound()
    {
        var budget = Budget(turnLimit: 1);

        budget.ObserveBatch([User("hi")]);

        await Assert.That(budget.IsFinal).IsTrue();
    }

    [Test]
    public async Task ZeroLimitStaysOpen()
    {
        var budget = Budget(turnLimit: 0);
        for (var i = 0; i < 8; i++)
        {
            budget.ObserveBatch(i == 0 ? [User("hi")] : [Tool($"r{i}")]);
        }

        await Assert.That(budget.Iteration).IsEqualTo(8);
        await Assert.That(budget.IsFinal).IsFalse();
    }

    private static IToolLoopBudget Budget(int turnLimit)
    {
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice") with
        {
            Tools = new AgentToolConfig { TurnLimit = turnLimit },
        };
        var session = new SessionId(config.Id, SessionId.DefaultSessionName);
        var scope = TestAgentConfigs.DataContextFactoryWith(config, session).Current!;
        return new ToolLoopBudget(scope);
    }

    private static ModelTurn User(string text) =>
        new ModelTurn(ModelRole.User, text, DateTimeOffset.UnixEpoch);

    private static ModelTurn Tool(string text) =>
        new ModelTurn(ModelRole.Tool, text, DateTimeOffset.UnixEpoch);
}
