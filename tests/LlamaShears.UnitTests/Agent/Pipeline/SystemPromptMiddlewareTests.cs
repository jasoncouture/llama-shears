using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Pipeline;
using LlamaShears.UnitTests.Agent.Core;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class SystemPromptMiddlewareTests
{
    [Test]
    public async Task RendersTheConfiguredTemplateOntoTheBag()
    {
        var scope = PipelineTestContext.ScopeFor();
        scope.SetItem(
            AgentConfig.DataKey,
            TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice") with { SystemPrompt = "MINIMAL.md" });
        var provider = Substitute.For<ISystemPromptProvider>();
        provider
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("you are alice"));
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        IAgentMiddleware middleware = new SystemPromptMiddleware(provider, scope, time);
        var context = PipelineTestContext.Create();
        var nextCalled = false;

        await middleware.InvokeAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(context.SystemPrompt).IsNotNull();
        await Assert.That(context.SystemPrompt!.Role).IsEqualTo(ModelRole.System);
        await Assert.That(context.SystemPrompt.Content).IsEqualTo("you are alice");
        await Assert.That(context.SystemPrompt.Timestamp).IsEqualTo(DateTimeOffset.UnixEpoch);
        await Assert.That(context.AgentContext.Turns).IsEmpty();
        await provider.Received(1).GetAsync(
            "MINIMAL.md",
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            context.TurnToken);
    }

    [Test]
    public async Task PassesNullTemplateWhenConfigOmitsSystemPrompt()
    {
        var provider = Substitute.For<ISystemPromptProvider>();
        provider
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("default persona"));
        IAgentMiddleware middleware = new SystemPromptMiddleware(
            provider,
            PipelineTestContext.ScopeFor(),
            TimeProvider.System);
        var context = PipelineTestContext.Create();

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await provider.Received(1).GetAsync(
            null,
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            context.TurnToken);
        await Assert.That(context.SystemPrompt!.Content).IsEqualTo("default persona");
    }

    [Test]
    public async Task UsesTurnTokenForTheProviderCall()
    {
        var provider = Substitute.For<ISystemPromptProvider>();
        provider
            .GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("persona"));
        IAgentMiddleware middleware = new SystemPromptMiddleware(
            provider,
            PipelineTestContext.ScopeFor(),
            TimeProvider.System);
        using var turn = new CancellationTokenSource();
        var context = PipelineTestContext.Create();
        context.TurnToken = turn.Token;

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await provider.Received(1).GetAsync(
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            turn.Token);
    }
}
