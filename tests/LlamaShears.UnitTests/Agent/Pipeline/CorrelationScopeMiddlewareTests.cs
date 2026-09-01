using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class CorrelationScopeMiddlewareTests
{
    [Test]
    public async Task AssignsAVersion7CorrelationIdBeforeNext()
    {
        IAgentMiddleware middleware = new CorrelationScopeMiddleware(
            NullLogger<CorrelationScopeMiddleware>.Instance);
        var context = PipelineTestContext.Create();
        Guid seen = Guid.Empty;

        await middleware.InvokeAsync(
            context,
            (ctx, _) =>
            {
                seen = ctx.CorrelationId;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(seen).IsNotEqualTo(Guid.Empty);
        await Assert.That(seen.Version).IsEqualTo(7);
        await Assert.That(context.CorrelationId).IsEqualTo(seen);
    }
}
