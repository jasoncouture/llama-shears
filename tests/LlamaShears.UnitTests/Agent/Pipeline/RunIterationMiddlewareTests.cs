using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Content;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Pipeline;
using LlamaShears.UnitTests.Agent.Core;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class RunIterationMiddlewareTests
{
    [Test]
    public async Task StoresTheIterationOutcomeOnTheContext()
    {
        var runner = Substitute.For<IAgentIterationRunner>();
        var expected = new IterationOutcome(Interrupted: false, ToolResultTurns: []);
        runner.RunAsync(Arg.Any<AgentPipelineContext>()).Returns(expected);
        var scope = PipelineTestContext.ScopeFor();
        IAgentMiddleware middleware = new RunIterationMiddleware(runner, scope);
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice"),
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch, ChannelId: "telegram:1")],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
        };
        var nextCalled = false;

        await middleware.InvokeAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(context.Outcome).IsEqualTo(expected);
        await Assert.That(context.SessionId).IsEqualTo(scope.GetCurrentSessionId());
        await Assert.That(context.ChannelId).IsEqualTo("telegram:1");
        await Assert.That(nextCalled).IsTrue();
        await runner.Received(1).RunAsync(context);
    }

    [Test]
    public async Task ModelSeesImageAttachmentsThenLiveContextDropsThem()
    {
        var image = new Attachment(AttachmentKind.Image, "image/png", "Zm9v");
        var turn = new ModelTurn(ModelRole.User, "see this", DateTimeOffset.UnixEpoch)
        {
            Attachments = [image],
        };
        ModelPrompt? seen = null;
        var runner = Substitute.For<IAgentIterationRunner>();
        runner.RunAsync(Arg.Any<AgentPipelineContext>()).Returns(call =>
        {
            seen = call.Arg<AgentPipelineContext>().Prompt;
            return new IterationOutcome(Interrupted: false, ToolResultTurns: []);
        });
        IAgentMiddleware middleware = new RunIterationMiddleware(runner, PipelineTestContext.ScopeFor());
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [turn]),
            [turn],
            CancellationToken.None)
        {
            Prompt = new ModelPrompt([turn]),
        };

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await Assert.That(seen).IsNotNull();
        await Assert.That(seen!.Turns[0].Attachments).IsEquivalentTo([image]);
        await Assert.That(context.AgentContext.Turns[0].Attachments.IsDefaultOrEmpty).IsTrue();
        await Assert.That(context.AgentContext.Turns[0].Content).IsEqualTo("see this");
    }

    [Test]
    public async Task StripsImageAttachmentsWhenTheIterationThrows()
    {
        var turn = new ModelTurn(ModelRole.User, "see this", DateTimeOffset.UnixEpoch)
        {
            Attachments = [new Attachment(AttachmentKind.Image, "image/png", "Zm9v")],
        };
        var runner = Substitute.For<IAgentIterationRunner>();
        runner.RunAsync(Arg.Any<AgentPipelineContext>()).Returns<IterationOutcome>(_ => throw new InvalidOperationException("boom"));
        IAgentMiddleware middleware = new RunIterationMiddleware(runner, PipelineTestContext.ScopeFor());
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [turn]),
            [turn],
            CancellationToken.None)
        {
            Prompt = new ModelPrompt([turn]),
        };

        await Assert.That(async () => await middleware.InvokeAsync(
                context,
                (_, _) => Task.CompletedTask,
                CancellationToken.None))
            .Throws<InvalidOperationException>();
        await Assert.That(context.AgentContext.Turns[0].Attachments.IsDefaultOrEmpty).IsTrue();
    }
}
