using LlamaShears.Core.Abstractions.Agent.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class AgentPipelineServiceCollectionExtensionsTests
{
    [Test]
    public async Task AddAgentPipelineRegistersTheFold()
    {
        var services = new ServiceCollection();
        services.AddAgentPipeline();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<IAgentPipeline>();

        await Assert.That(pipeline).IsTypeOf<AgentPipeline>();
    }

    [Test]
    public async Task AddAgentMiddlewareRegistersTheInvokerAndTheStep()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IList<string>>([]);
        services.AddAgentMiddleware<RecordingAgentMiddleware>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var pipeline = scope.ServiceProvider.GetRequiredService<IAgentPipeline>();
        var steps = scope.ServiceProvider.GetServices<IAgentMiddleware>().ToArray();

        await Assert.That(pipeline).IsTypeOf<AgentPipeline>();
        await Assert.That(steps).HasSingleItem();
        await Assert.That(steps[0]).IsTypeOf<RecordingAgentMiddleware>();
    }

    [Test]
    public async Task AddAgentPipelineIsIdempotent()
    {
        var services = new ServiceCollection();
        services.AddAgentPipeline();
        services.AddAgentPipeline();

        var descriptors = services.Where(d => d.ServiceType == typeof(IAgentPipeline)).ToArray();
        await Assert.That(descriptors).HasSingleItem();
    }
}
