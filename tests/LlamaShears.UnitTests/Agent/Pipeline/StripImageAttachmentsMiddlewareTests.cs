using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Content;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Pipeline;
using LlamaShears.UnitTests.Agent.Core;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class StripImageAttachmentsMiddlewareTests
{
    [Test]
    public async Task DropsImageAttachmentsAfterNext()
    {
        var image = new Attachment(AttachmentKind.Image, "image/png", "Zm9v");
        var turn = new ModelTurn(ModelRole.User, "see this", DateTimeOffset.UnixEpoch)
        {
            Attachments = [image],
        };
        IAgentMiddleware middleware = new StripImageAttachmentsMiddleware();
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [turn]),
            [turn],
            CancellationToken.None);
        var sawImagesDuringNext = false;

        await middleware.InvokeAsync(
            context,
            (_, _) =>
            {
                sawImagesDuringNext = context.AgentContext.Turns[0].Attachments.Length == 1;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(sawImagesDuringNext).IsTrue();
        await Assert.That(context.AgentContext.Turns[0].Attachments.IsDefaultOrEmpty).IsTrue();
        await Assert.That(context.AgentContext.Turns[0].Content).IsEqualTo("see this");
    }

    [Test]
    public async Task DropsImageAttachmentsWhenNextThrows()
    {
        var turn = new ModelTurn(ModelRole.User, "see this", DateTimeOffset.UnixEpoch)
        {
            Attachments = [new Attachment(AttachmentKind.Image, "image/png", "Zm9v")],
        };
        IAgentMiddleware middleware = new StripImageAttachmentsMiddleware();
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [turn]),
            [turn],
            CancellationToken.None);

        await Assert.That(async () => await middleware.InvokeAsync(
                context,
                (_, _) => throw new InvalidOperationException("boom"),
                CancellationToken.None))
            .Throws<InvalidOperationException>();
        await Assert.That(context.AgentContext.Turns[0].Attachments.IsDefaultOrEmpty).IsTrue();
    }
}
