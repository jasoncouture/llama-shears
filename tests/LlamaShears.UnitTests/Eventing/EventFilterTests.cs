using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Eventing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LlamaShears.UnitTests.Eventing;

public sealed class EventFilterTests
{
    private static readonly EventType TestType = new("test", "filter");

    [Test]
    public async Task NoFiltersDeliversBothLegs()
    {
        var (publisher, bus, recorder) = BuildHarness();

        await publisher.PublishAsync(TestType, new Payload("hi"), Guid.NewGuid(), CancellationToken.None);

        await Assert.That(recorder.FireAndForgetCount).IsEqualTo(1);
        await Assert.That(recorder.AwaitedCount).IsEqualTo(1);
        GC.KeepAlive(bus);
    }

    [Test]
    public async Task FilterDenyingFireAndForgetSkipsThatLegOnly()
    {
        var filter = Substitute.For<IEventFilter>();
        filter.GetDeniedModesAsync(Arg.Any<IEventEnvelope<object>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(EventDeliveryMask.FireAndForget));

        var (publisher, bus, recorder) = BuildHarness(filter);

        await publisher.PublishAsync(TestType, new Payload("x"), Guid.NewGuid(), CancellationToken.None);

        await Assert.That(recorder.FireAndForgetCount).IsEqualTo(0);
        await Assert.That(recorder.AwaitedCount).IsEqualTo(1);
        GC.KeepAlive(bus);
    }

    [Test]
    public async Task FilterDenyingAwaitedSkipsThatLegOnly()
    {
        var filter = Substitute.For<IEventFilter>();
        filter.GetDeniedModesAsync(Arg.Any<IEventEnvelope<object>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(EventDeliveryMask.Awaited));

        var (publisher, bus, recorder) = BuildHarness(filter);

        await publisher.PublishAsync(TestType, new Payload("x"), Guid.NewGuid(), CancellationToken.None);

        await Assert.That(recorder.FireAndForgetCount).IsEqualTo(1);
        await Assert.That(recorder.AwaitedCount).IsEqualTo(0);
        GC.KeepAlive(bus);
    }

    [Test]
    public async Task FilterDenyingBothDropsTheEvent()
    {
        var filter = Substitute.For<IEventFilter>();
        filter.GetDeniedModesAsync(Arg.Any<IEventEnvelope<object>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(EventDeliveryMask.Both));

        var (publisher, bus, recorder) = BuildHarness(filter);

        await publisher.PublishAsync(TestType, new Payload("x"), Guid.NewGuid(), CancellationToken.None);

        await Assert.That(recorder.FireAndForgetCount).IsEqualTo(0);
        await Assert.That(recorder.AwaitedCount).IsEqualTo(0);
        GC.KeepAlive(bus);
    }

    [Test]
    public async Task MultipleFiltersAreOrredTogether()
    {
        var fnf = Substitute.For<IEventFilter>();
        fnf.GetDeniedModesAsync(Arg.Any<IEventEnvelope<object>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(EventDeliveryMask.FireAndForget));
        var awaited = Substitute.For<IEventFilter>();
        awaited.GetDeniedModesAsync(Arg.Any<IEventEnvelope<object>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(EventDeliveryMask.Awaited));

        var (publisher, bus, recorder) = BuildHarness(fnf, awaited);

        await publisher.PublishAsync(TestType, new Payload("x"), Guid.NewGuid(), CancellationToken.None);

        await Assert.That(recorder.FireAndForgetCount).IsEqualTo(0);
        await Assert.That(recorder.AwaitedCount).IsEqualTo(0);
        GC.KeepAlive(bus);
    }

    [Test]
    public async Task FiltersStopBeingCalledOnceBothLegsAreDenied()
    {
        var first = Substitute.For<IEventFilter>();
        first.GetDeniedModesAsync(Arg.Any<IEventEnvelope<object>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(EventDeliveryMask.Both));
        var second = Substitute.For<IEventFilter>();
        second.GetDeniedModesAsync(Arg.Any<IEventEnvelope<object>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(EventDeliveryMask.None));

        var (publisher, bus, _) = BuildHarness(first, second);

        await publisher.PublishAsync(TestType, new Payload("x"), Guid.NewGuid(), CancellationToken.None);

        await second.DidNotReceive().GetDeniedModesAsync(Arg.Any<IEventEnvelope<object>>(), Arg.Any<CancellationToken>());
        GC.KeepAlive(bus);
    }

    [Test]
    public async Task ThrowingFilterPropagatesAndSkipsBothLegs()
    {
        var filter = Substitute.For<IEventFilter>();
        filter.GetDeniedModesAsync(Arg.Any<IEventEnvelope<object>>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<EventDeliveryMask>>(_ => throw new InvalidOperationException("filter blew up"));

        var (publisher, bus, recorder) = BuildHarness(filter);

        await Assert.That(async () =>
            await publisher.PublishAsync(TestType, new Payload("x"), Guid.NewGuid(), CancellationToken.None))
            .Throws<InvalidOperationException>();

        await Assert.That(recorder.FireAndForgetCount).IsEqualTo(0);
        await Assert.That(recorder.AwaitedCount).IsEqualTo(0);
        GC.KeepAlive(bus);
    }

    [Test]
    public async Task FireAndForgetCancelledHandlerDoesNotCrashHost()
    {
        // Regression: when /interrupt cancelled a per-turn CTS that was
        // the live publish CT, the FAF wrapper's ThrowIfCancellationRequested
        // fired and MessagePipe's TaskExtensions.Forget re-threw the OCE on
        // the threadpool, killing the host. The wrapper now catches OCE
        // when the CT is cancelled (mode-independent), so FAF dispatch
        // never escapes an OCE into Forget.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
        var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        bus.Subscribe<Payload>(
            pattern: null,
            EventDeliveryMode.FireAndForget,
            new ThrowOnCancellationHandler());

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Awaited leg surfaces OCE back to the caller (publish reasserts
        // cancellation at the tail). Catch + assert: that's the only OCE
        // that should appear; the FAF leg's wrapper must not have crashed
        // the host on the threadpool before this point.
        await Assert.That(async () =>
            await publisher.PublishAsync(TestType, new Payload("x"), Guid.NewGuid(), cancellationTokenSource.Token))
            .Throws<OperationCanceledException>();

        GC.KeepAlive(bus);
    }

    [Test]
    public async Task AwaitedPublishReassertsCancellationAfterHandlersSwallow()
    {
        // Per-handler swallow of OCE-on-cancellation means an awaited
        // dispatch can complete "successfully" from MessagePipe's POV
        // even though the caller's CT was tripped. PublishAsync is
        // expected to ThrowIfCancellationRequested at the tail so
        // `await PublishAsync` still surfaces cancellation to callers.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
        var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        bus.Subscribe<Payload>(
            pattern: null,
            EventDeliveryMode.Awaited,
            new ThrowOnCancellationHandler());

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.That(async () =>
            await publisher.PublishAsync(TestType, new Payload("x"), Guid.NewGuid(), cancellationTokenSource.Token))
            .Throws<OperationCanceledException>();

        GC.KeepAlive(bus);
    }

    [Test]
    public async Task FilterReceivesEnvelopeWithFireAndForgetMode()
    {
        EventDeliveryMode? observed = null;
        var filter = Substitute.For<IEventFilter>();
        filter.GetDeniedModesAsync(Arg.Any<IEventEnvelope<object>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                observed = call.Arg<IEventEnvelope<object>>().DeliveryMode;
                return ValueTask.FromResult(EventDeliveryMask.None);
            });

        var (publisher, bus, _) = BuildHarness(filter);

        await publisher.PublishAsync(TestType, new Payload("x"), Guid.NewGuid(), CancellationToken.None);

        await Assert.That(observed).IsEqualTo(EventDeliveryMode.FireAndForget);
        GC.KeepAlive(bus);
    }

    private static (IEventPublisher publisher, IEventBus bus, RecordingHandler recorder) BuildHarness(params IEventFilter[] filters)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
        foreach (var filter in filters)
        {
            services.AddSingleton(filter);
        }
        var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        var recorder = new RecordingHandler();
        bus.Subscribe<Payload>(pattern: null, EventDeliveryMode.FireAndForget, recorder.AsFireAndForget());
        bus.Subscribe<Payload>(pattern: null, EventDeliveryMode.Awaited, recorder.AsAwaited());
        return (publisher, bus, recorder);
    }

    private sealed record Payload(string Value);

    private sealed class ThrowOnCancellationHandler : IEventHandler<Payload>
    {
        public ValueTask HandleAsync(IEventEnvelope<Payload> envelope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingHandler
    {
        private int _fnf;
        private int _awaited;

        public int FireAndForgetCount => _fnf;
        public int AwaitedCount => _awaited;

        public IEventHandler<Payload> AsFireAndForget() => new ModeRecorder(this, EventDeliveryMode.FireAndForget);
        public IEventHandler<Payload> AsAwaited() => new ModeRecorder(this, EventDeliveryMode.Awaited);

        private void Record(EventDeliveryMode mode)
        {
            if (mode == EventDeliveryMode.FireAndForget) Interlocked.Increment(ref _fnf);
            else Interlocked.Increment(ref _awaited);
        }

        private sealed class ModeRecorder : IEventHandler<Payload>
        {
            private readonly RecordingHandler _owner;
            private readonly EventDeliveryMode _mode;

            public ModeRecorder(RecordingHandler owner, EventDeliveryMode mode)
            {
                _owner = owner;
                _mode = mode;
            }

            public ValueTask HandleAsync(IEventEnvelope<Payload> envelope, CancellationToken cancellationToken)
            {
                _owner.Record(_mode);
                return ValueTask.CompletedTask;
            }
        }
    }
}
