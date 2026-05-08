using System.Collections.Immutable;
using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class InferenceRunnerToolDispatchTests
{
    [Test]
    public async Task DispatcherFiresAsToolFragmentsArriveAndOutcomeCarriesResultsInOrder()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();

        var calls = new[]
        {
            new ToolCall("llamashears", "file_read", "{\"path\":\"a\"}", "1"),
            new ToolCall("llamashears", "file_read", "{\"path\":\"b\"}", "2"),
            new ToolCall("llamashears", "file_read", "{\"path\":\"c\"}", "3"),
        };
        var model = ScriptedLanguageModel.WithFragments(
            ScriptedLanguageModel.ToolCallFragment("llamashears", "file_read", "{\"path\":\"a\"}", "1"),
            ScriptedLanguageModel.ToolCallFragment("llamashears", "file_read", "{\"path\":\"b\"}", "2"),
            ScriptedLanguageModel.ToolCallFragment("llamashears", "file_read", "{\"path\":\"c\"}", "3"));

        var dispatched = new List<string>();
        var dispatcher = Substitute.For<IToolCallDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<ToolCall>(), Arg.Any<ImmutableArray<ToolGroup>>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var c = call.Arg<ToolCall>();
                lock (dispatched)
                {
                    dispatched.Add(c.CallId!);
                }
                return new ValueTask<ToolCallResult>(new ToolCallResult($"result-{c.CallId}", IsError: false));
            });

        var runner = new InferenceRunner(publisher, dispatcher, TimeProvider.System);
        var outcome = await runner.RunAsync(
            eventId: "alpha",
            model: model,
            prompt: new ModelPrompt([new ModelTurn(ModelRole.User, "go", DateTimeOffset.UnixEpoch)]),
            options: new PromptOptions(Tools: BuildToolsAdvertisement()),
            emitTurns: false,
            correlationId: Guid.CreateVersion7(),
            cancellationToken: CancellationToken.None);

        await Assert.That(outcome.ToolCalls.Length).IsEqualTo(3);
        await Assert.That(outcome.ToolResults.Length).IsEqualTo(3);
        for (var i = 0; i < calls.Length; i++)
        {
            await Assert.That(outcome.ToolCalls[i].CallId).IsEqualTo(calls[i].CallId);
            await Assert.That(outcome.ToolResults[i].Content).IsEqualTo($"result-{calls[i].CallId}");
        }
        await Assert.That(dispatched.Count).IsEqualTo(3);
    }

    [Test]
    public async Task OptionsWithoutToolsStillDispatchesAndDispatcherRejects()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var dispatcher = Substitute.For<IToolCallDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<ToolCall>(), Arg.Any<ImmutableArray<ToolGroup>>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ToolCallResult>(new ToolCallResult("not advertised", IsError: true)));

        var model = ScriptedLanguageModel.WithFragments(
            ScriptedLanguageModel.ToolCallFragment("llamashears", "file_read", "{}", "1"));

        var runner = new InferenceRunner(publisher, dispatcher, TimeProvider.System);
        var outcome = await runner.RunAsync(
            eventId: "alpha",
            model: model,
            prompt: new ModelPrompt([new ModelTurn(ModelRole.User, "go", DateTimeOffset.UnixEpoch)]),
            options: null,
            emitTurns: false,
            correlationId: Guid.CreateVersion7(),
            cancellationToken: CancellationToken.None);

        await Assert.That(outcome.ToolCalls.Length).IsEqualTo(1);
        await Assert.That(outcome.ToolResults.Length).IsEqualTo(1);
        await Assert.That(outcome.ToolResults[0].IsError).IsTrue();
    }

    private static ImmutableArray<ToolGroup> BuildToolsAdvertisement() =>
        [new ToolGroup("llamashears", [new ToolDescriptor("file_read", "Read a file.", [])])];

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
        return services.BuildServiceProvider();
    }
}
