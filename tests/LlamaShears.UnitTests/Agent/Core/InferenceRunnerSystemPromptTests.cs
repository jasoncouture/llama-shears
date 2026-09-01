using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class InferenceRunnerSystemPromptTests
{
    [Test]
    public async Task PrependsRenderedSystemTurnWhenTemplateIsSet()
    {
        var systemPrompt = Substitute.For<ISystemPromptProvider>();
        systemPrompt
            .GetAsync("COMPACTION.md", Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("summarize"));
        var model = ScriptedLanguageModel.WithText("ok");
        var runner = CreateRunner(systemPrompt, model, new FakeTimeProvider(DateTimeOffset.UnixEpoch));

        await runner.RunAsync(
            prompt: new ModelPrompt([new ModelTurn(ModelRole.User, "go", DateTimeOffset.UnixEpoch)]),
            options: new PromptOptions(SystemPromptTemplate: "COMPACTION.md"),
            cancellationToken: CancellationToken.None);

        await Assert.That(model.LastPrompt).IsNotNull();
        await Assert.That(model.LastPrompt!.Turns[0].Role).IsEqualTo(ModelRole.System);
        await Assert.That(model.LastPrompt.Turns[0].Content).IsEqualTo("summarize");
        await Assert.That(model.LastPrompt.Turns[0].Timestamp).IsEqualTo(DateTimeOffset.UnixEpoch);
        await Assert.That(model.LastPrompt.Turns[1].Content).IsEqualTo("go");
    }

    [Test]
    public async Task SkipsSystemTurnWhenTemplateIsNull()
    {
        var systemPrompt = Substitute.For<ISystemPromptProvider>();
        var model = ScriptedLanguageModel.WithText("ok");
        var runner = CreateRunner(systemPrompt, model, TimeProvider.System);

        await runner.RunAsync(
            prompt: new ModelPrompt([new ModelTurn(ModelRole.User, "go", DateTimeOffset.UnixEpoch)]),
            options: new PromptOptions(SystemPromptTemplate: null),
            cancellationToken: CancellationToken.None);

        await systemPrompt.DidNotReceive()
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>());
        await Assert.That(model.LastPrompt!.Turns[0].Role).IsEqualTo(ModelRole.User);
        await Assert.That(model.LastPrompt.Turns[0].Content).IsEqualTo("go");
    }

    private static InferenceRunner CreateRunner(
        ISystemPromptProvider systemPrompt,
        ILanguageModel model,
        TimeProvider time)
        => new(
            Substitute.For<IEventBus>(),
            Substitute.For<IToolCallDispatcher>(),
            time,
            Substitute.For<IPromptContextProvider>(),
            systemPrompt,
            Substitute.For<IMemorySearcher>(),
            TestAgentConfigs.DataContextFactoryWith(TestAgentConfigs.WithHeartbeat(TimeSpan.Zero)).Current!,
            model,
            NullLogger<InferenceRunner>.Instance);
}
