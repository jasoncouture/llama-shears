using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Eventing;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.UnitTests.Eventing;

public sealed class AddEventHandlerTests
{
    [Test]
    public async Task AddEventHandlerRegistersTheImplementationAsAResolvableSingleton()
    {
        var services = new ServiceCollection();
        services.AddEventHandler<TestPayload, TestHandler>();
        await using var provider = services.BuildServiceProvider();

        var resolvedA = provider.GetRequiredService<TestHandler>();
        var resolvedB = provider.GetRequiredService<TestHandler>();

        await Assert.That(resolvedA).IsSameReferenceAs(resolvedB);
    }

    [Test]
    public async Task IEventHandlerOfMessageResolvesToTheSameSingletonInstance()
    {
        var services = new ServiceCollection();
        services.AddEventHandler<TestPayload, TestHandler>();
        await using var provider = services.BuildServiceProvider();

        var direct = provider.GetRequiredService<TestHandler>();
        var asInterface = provider.GetRequiredService<IEnumerable<IEventHandler<TestPayload>>>().Single();

        await Assert.That(asInterface).IsSameReferenceAs(direct);
    }

    [Test]
    public async Task CallingAddEventHandlerTwiceForTheSamePairDoesNotDuplicateTheRegistration()
    {
        var services = new ServiceCollection();
        services.AddEventHandler<TestPayload, TestHandler>();
        services.AddEventHandler<TestPayload, TestHandler>();
        await using var provider = services.BuildServiceProvider();

        var handlers = provider.GetRequiredService<IEnumerable<IEventHandler<TestPayload>>>();

        await Assert.That(handlers).Count().IsEqualTo(1);
    }

    [Test]
    public async Task TwoDistinctHandlersForTheSameMessageBothResolveAsIEventHandlerOfMessage()
    {
        var services = new ServiceCollection();
        services.AddEventHandler<TestPayload, TestHandler>();
        services.AddEventHandler<TestPayload, OtherHandler>();
        await using var provider = services.BuildServiceProvider();

        var handlers = provider.GetRequiredService<IEnumerable<IEventHandler<TestPayload>>>().ToArray();

        await Assert.That(handlers).Count().IsEqualTo(2);
        await Assert.That(handlers.OfType<TestHandler>().Count()).IsEqualTo(1);
        await Assert.That(handlers.OfType<OtherHandler>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task OneHandlerImplementingTwoMessageTypesIsASingleSharedSingleton()
    {
        var services = new ServiceCollection();
        services.AddEventHandler<TestPayload, MultiHandler>();
        services.AddEventHandler<OtherPayload, MultiHandler>();
        await using var provider = services.BuildServiceProvider();

        var direct = provider.GetRequiredService<MultiHandler>();
        var asPayload = provider.GetRequiredService<IEnumerable<IEventHandler<TestPayload>>>().Single();
        var asOther = provider.GetRequiredService<IEnumerable<IEventHandler<OtherPayload>>>().Single();

        await Assert.That(asPayload).IsSameReferenceAs(direct);
        await Assert.That(asOther).IsSameReferenceAs(direct);
    }

    private sealed record TestPayload(string Value);

    private sealed record OtherPayload(int Value);

    private sealed class TestHandler : IEventHandler<TestPayload>
    {
        public ValueTask HandleAsync(IEventEnvelope<TestPayload> envelope, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class OtherHandler : IEventHandler<TestPayload>
    {
        public ValueTask HandleAsync(IEventEnvelope<TestPayload> envelope, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class MultiHandler : IEventHandler<TestPayload>, IEventHandler<OtherPayload>
    {
        public ValueTask HandleAsync(IEventEnvelope<TestPayload> envelope, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask HandleAsync(IEventEnvelope<OtherPayload> envelope, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
