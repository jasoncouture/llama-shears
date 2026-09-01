using System.Collections.Immutable;
using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class InferenceRunnerToolDispatchTests
{
    [Test]
    public async Task ReturnsToolCallsWithoutDispatchingThem()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventBus>();
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

        var runner = new InferenceRunner(
            publisher,
            TimeProvider.System,
            model,
            NullLogger<InferenceRunner>.Instance);
        var outcome = await runner.RunAsync(
            prompt: new ModelPrompt([new ModelTurn(ModelRole.User, "go", DateTimeOffset.UnixEpoch)]),
            options: new PromptOptions(Tools: BuildToolsAdvertisement()),
            sessionId: new SessionId("alice", SessionId.DefaultSessionName),
            correlationId: Guid.CreateVersion7(),
            cancellationToken: CancellationToken.None);

        await Assert.That(outcome.ToolCalls.Length).IsEqualTo(3);
        for (var i = 0; i < calls.Length; i++)
        {
            await Assert.That(outcome.ToolCalls[i].CallId).IsEqualTo(calls[i].CallId);
        }
    }

    [Test]
    public async Task OptionsWithoutToolsStillReturnsTheCall()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventBus>();
        var model = ScriptedLanguageModel.WithFragments(
            ScriptedLanguageModel.ToolCallFragment("llamashears", "file_read", "{}", "1"));

        var runner = new InferenceRunner(
            publisher,
            TimeProvider.System,
            model,
            NullLogger<InferenceRunner>.Instance);
        var outcome = await runner.RunAsync(
            prompt: new ModelPrompt([new ModelTurn(ModelRole.User, "go", DateTimeOffset.UnixEpoch)]),
            options: null,
            sessionId: new SessionId("alice", SessionId.DefaultSessionName),
            correlationId: Guid.CreateVersion7(),
            cancellationToken: CancellationToken.None);

        await Assert.That(outcome.ToolCalls.Length).IsEqualTo(1);
        await Assert.That(outcome.ToolCalls[0].CallId).IsEqualTo("1");
    }

    private static ImmutableArray<ToolGroup> BuildToolsAdvertisement() =>
        [new ToolGroup("llamashears", [new ToolDescriptor("file_read", "Read a file.", [], new ToolDescriptorAnnotations("file_read"))])];

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
        return services.BuildServiceProvider();
    }
}
