using LlamaShears.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LlamaShears.UnitTests.Hosting;

public sealed class HostStartupTaskRunnerTests
{
    [Test]
    public async Task RunsEveryRegisteredTaskInRegistrationOrder()
    {
        var order = new List<string>();
        var services = new ServiceCollection()
            .AddSingleton(order)
            .AddHostStartupTask<RecordingTask<TaskA>>()
            .AddHostStartupTask<RecordingTask<TaskB>>()
            .AddHostStartupTask<RecordingTask<TaskC>>()
            .AddHostStartupTaskRunner()
            .BuildServiceProvider();

        var runner = services.GetRequiredService<IEnumerable<IHostedService>>()
            .OfType<HostStartupTaskRunner>()
            .Single();

        await runner.StartAsync(CancellationToken.None);

        await Assert.That(order).IsEquivalentTo(["TaskA", "TaskB", "TaskC"]);
    }

    [Test]
    public async Task TasksResolveFromAFreshScopeEachRun()
    {
        var services = new ServiceCollection()
            .AddScoped<ScopedDependency>()
            .AddHostStartupTask<ScopeProbeTask>()
            .AddHostStartupTaskRunner()
            .BuildServiceProvider();

        var runner = services.GetRequiredService<IEnumerable<IHostedService>>()
            .OfType<HostStartupTaskRunner>()
            .Single();

        ScopeProbeTask.LastObservedScopeId = Guid.Empty;
        await runner.StartAsync(CancellationToken.None);
        var firstRun = ScopeProbeTask.LastObservedScopeId;

        ScopeProbeTask.LastObservedScopeId = Guid.Empty;
        await runner.StartAsync(CancellationToken.None);
        var secondRun = ScopeProbeTask.LastObservedScopeId;

        await Assert.That(firstRun).IsNotEqualTo(Guid.Empty);
        await Assert.That(secondRun).IsNotEqualTo(Guid.Empty);
        await Assert.That(firstRun).IsNotEqualTo(secondRun);
    }

    [Test]
    public async Task ThrowingTaskAbortsSubsequentTasks()
    {
        var order = new List<string>();
        var services = new ServiceCollection()
            .AddSingleton(order)
            .AddHostStartupTask<RecordingTask<TaskA>>()
            .AddHostStartupTask<ThrowingTask>()
            .AddHostStartupTask<RecordingTask<TaskC>>()
            .AddHostStartupTaskRunner()
            .BuildServiceProvider();

        var runner = services.GetRequiredService<IEnumerable<IHostedService>>()
            .OfType<HostStartupTaskRunner>()
            .Single();

        await Assert.That(async () => await runner.StartAsync(CancellationToken.None))
            .Throws<InvalidOperationException>();

        await Assert.That(order).IsEquivalentTo(["TaskA"]);
    }

    [Test]
    public async Task PreCancelledTokenSkipsAllTasks()
    {
        var order = new List<string>();
        var services = new ServiceCollection()
            .AddSingleton(order)
            .AddHostStartupTask<RecordingTask<TaskA>>()
            .AddHostStartupTaskRunner()
            .BuildServiceProvider();

        var runner = services.GetRequiredService<IEnumerable<IHostedService>>()
            .OfType<HostStartupTaskRunner>()
            .Single();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(async () => await runner.StartAsync(cts.Token))
            .Throws<OperationCanceledException>();

        await Assert.That(order).IsEmpty();
    }

    [Test]
    public async Task StopAsyncIsANoOp()
    {
        var services = new ServiceCollection()
            .AddHostStartupTaskRunner()
            .BuildServiceProvider();

        var runner = services.GetRequiredService<IEnumerable<IHostedService>>()
            .OfType<HostStartupTaskRunner>()
            .Single();

        await runner.StopAsync(CancellationToken.None);
    }

    private sealed class TaskA;

    private sealed class TaskB;

    private sealed class TaskC;

    private sealed class RecordingTask<TMarker> : IHostStartupTask
    {
        private readonly List<string> _order;

        public RecordingTask(List<string> order)
        {
            _order = order;
        }

        public ValueTask StartAsync(CancellationToken cancellationToken)
        {
            _order.Add(typeof(TMarker).Name);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingTask : IHostStartupTask
    {
        public ValueTask StartAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("boom");
    }

    private sealed class ScopedDependency
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class ScopeProbeTask : IHostStartupTask
    {
        public static Guid LastObservedScopeId;

        private readonly ScopedDependency _scoped;

        public ScopeProbeTask(ScopedDependency scoped)
        {
            _scoped = scoped;
        }

        public ValueTask StartAsync(CancellationToken cancellationToken)
        {
            LastObservedScopeId = _scoped.Id;
            return ValueTask.CompletedTask;
        }
    }
}
