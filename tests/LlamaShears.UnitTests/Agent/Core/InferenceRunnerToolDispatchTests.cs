using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class InferenceRunnerToolDispatchTests
{
    [Test]
    public async Task DispatcherFiresAsToolFragmentsArriveAndOutcomeCarriesResultsInOrder()
    {
        // Three tool fragments in a row. Assert each was dispatched and
        // the final InferenceOutcome.ToolResults aligns with ToolCalls
        // in count and order.
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
        ValueTask<ToolCallResult> Dispatch(ToolCall call, CancellationToken cancellationToken)
        {
            lock (dispatched)
            {
                dispatched.Add(call.CallId!);
            }
            return new ValueTask<ToolCallResult>(new ToolCallResult($"result-{call.CallId}", IsError: false));
        }

        var runner = new InferenceRunner(publisher, TimeProvider.System);
        var outcome = await runner.RunAsync(
            eventId: "alpha",
            model: model,
            prompt: new ModelPrompt([new ModelTurn(ModelRole.User, "go", DateTimeOffset.UnixEpoch)]),
            options: null,
            emitTurns: false,
            correlationId: Guid.CreateVersion7(),
            dispatchTool: Dispatch,
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
    public async Task NullDispatcherLeavesToolResultsEmpty()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();

        var model = ScriptedLanguageModel.WithFragments(
            ScriptedLanguageModel.ToolCallFragment("llamashears", "file_read", "{}", "1"));

        var runner = new InferenceRunner(publisher, TimeProvider.System);
        var outcome = await runner.RunAsync(
            eventId: "alpha",
            model: model,
            prompt: new ModelPrompt([new ModelTurn(ModelRole.User, "go", DateTimeOffset.UnixEpoch)]),
            options: null,
            emitTurns: false,
            correlationId: Guid.CreateVersion7(),
            dispatchTool: null,
            cancellationToken: CancellationToken.None);

        await Assert.That(outcome.ToolCalls.Length).IsEqualTo(1);
        await Assert.That(outcome.ToolResults.IsDefaultOrEmpty).IsTrue();
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
        return services.BuildServiceProvider();
    }
}
