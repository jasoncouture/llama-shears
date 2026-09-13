using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Provider;
using LlamaShears.UnitTests.Agent.Core;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Provider;

public sealed class ModelPromptAssemblerTests
{
    private static readonly DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    [Test]
    public async Task PrependsSystemAndEphemeralBeforeTheTrailingUserCluster()
    {
        IModelPromptAssembler assembler = BuildAssembler(
            systemBody: "persona",
            ephemeralBody: "now-block");
        var kicker = new ModelTurn(ModelRole.User, "kicker", _now);
        var transcript = new ModelTurn(ModelRole.User, "transcript", _now);

        var result = await assembler.AssembleAsync([kicker, transcript], CancellationToken.None);

        await Assert.That(result.Turns.Count).IsEqualTo(4);
        await Assert.That(result.Turns[0].Role).IsEqualTo(ModelRole.System);
        await Assert.That(result.Turns[0].Content).IsEqualTo("persona");
        await Assert.That(result.Turns[1].Role).IsEqualTo(ModelRole.SystemEphemeral);
        await Assert.That(result.Turns[1].Content).IsEqualTo("now-block");
        await Assert.That(result.Turns[1].Ephemeral).IsTrue();
        await Assert.That(result.Turns[2]).IsEqualTo(kicker);
        await Assert.That(result.Turns[3]).IsEqualTo(transcript);
    }

    [Test]
    public async Task OmitsEphemeralWhenTheTemplateRendersBlank()
    {
        IModelPromptAssembler assembler = BuildAssembler(
            systemBody: "persona",
            ephemeralBody: "   ");
        var user = new ModelTurn(ModelRole.User, "hi", _now);

        var result = await assembler.AssembleAsync([user], CancellationToken.None);

        await Assert.That(result.Turns.Count).IsEqualTo(2);
        await Assert.That(result.Turns[0].Role).IsEqualTo(ModelRole.System);
        await Assert.That(result.Turns[1]).IsEqualTo(user);
    }

    [Test]
    public async Task UsesTheConfiguredSystemAndPromptContextTemplateNames()
    {
        var systemPrompt = Substitute.For<ISystemPromptProvider>();
        systemPrompt
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("persona"));
        var promptContext = Substitute.For<IPromptContextProvider>();
        promptContext
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("now-block"));
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice") with
        {
            SystemPrompt = "COMPACTION.md",
            PromptContext = "PROMPT.md",
        };
        IModelPromptAssembler assembler = BuildAssembler(systemPrompt, promptContext, config);

        await assembler.AssembleAsync(
            [new ModelTurn(ModelRole.User, "kicker", _now)],
            CancellationToken.None);

        await systemPrompt.Received(1).GetAsync(
            "COMPACTION.md",
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            Arg.Any<CancellationToken>());
        await promptContext.Received(1).GetAsync(
            "PROMPT.md",
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            Arg.Any<CancellationToken>());
    }

    private static IModelPromptAssembler BuildAssembler(string systemBody, string? ephemeralBody)
    {
        var systemPrompt = Substitute.For<ISystemPromptProvider>();
        systemPrompt
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(systemBody));
        var promptContext = Substitute.For<IPromptContextProvider>();
        promptContext
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(ephemeralBody));
        return BuildAssembler(systemPrompt, promptContext, TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice"));
    }

    private static IModelPromptAssembler BuildAssembler(
        ISystemPromptProvider systemPrompt,
        IPromptContextProvider promptContext,
        AgentConfig config)
    {
        var scope = TestAgentConfigs.DataContextFactoryWith(config).Current!;
        return new ModelPromptAssembler(
            systemPrompt,
            promptContext,
            scope,
            new FakeTimeProvider(_now));
    }
}
