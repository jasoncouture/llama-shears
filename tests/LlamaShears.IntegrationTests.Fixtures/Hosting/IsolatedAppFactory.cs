using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.IntegrationTests.Hosting;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> wrapper that points
/// every <c>Paths:*</c> config key at a unique temp directory so a test
/// can never see, write to, or load from the developer's
/// <c>~/.llama-shears/</c> tree.
/// <para>
/// Each instance creates a fresh <see cref="DataRoot"/> on construction
/// and recursively deletes it on dispose. Tests that need to seed agent
/// JSON files (or any other on-disk state) can use <see cref="DataRoot"/>
/// directly.
/// </para>
/// </summary>
public sealed class IsolatedAppFactory : WebApplicationFactory<Program>
{
    public IsolatedAppFactory()
    {
        DataRoot = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "llama-shears-it-" + Guid.NewGuid().ToString("N")))
            .FullName;
        AgentsRoot = Directory.CreateDirectory(Path.Combine(DataRoot, "agents")).FullName;
        WorkspaceRoot = Directory.CreateDirectory(Path.Combine(DataRoot, "workspace")).FullName;
        TemplatesRoot = Directory.CreateDirectory(Path.Combine(DataRoot, "templates")).FullName;
        ProviderFactory = new StubProviderFactory();
    }

    public string DataRoot { get; }

    public string AgentsRoot { get; }

    public string WorkspaceRoot { get; }

    public string TemplatesRoot { get; }

    /// <summary>
    /// The single <see cref="IProviderFactory"/> visible to the test
    /// host. Real providers (Ollama, etc.) are stripped from DI on host
    /// start, so this is the only path an agent can reach.
    /// </summary>
    public StubProviderFactory ProviderFactory { get; }

    /// <summary>
    /// Convenience accessor for the canned model returned by
    /// <see cref="ProviderFactory"/>; tests assert on its
    /// <see cref="StubLanguageModel.InvocationCount"/> when they want to
    /// verify whether the agent loop reached the model.
    /// </summary>
    public StubLanguageModel Model => ProviderFactory.Model;

    public string SeedAgent(string id, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var path = Path.Combine(AgentsRoot, id + ".json");
        File.WriteAllText(path, json);
        return path;
    }

    /// <summary>
    /// Publishes a system tick so the config supervisor rescans the seeded
    /// agents directory immediately. The production tick service runs on a
    /// 30-second interval, which is too slow for tests.
    /// </summary>
    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        var publisher = Services.GetRequiredService<IEventBus>();
        await publisher.PublishAsync(
            Event.WellKnown.Host.Tick,
            new SystemTick(DateTimeOffset.UtcNow),
            cancellationToken);
    }

    /// <summary>
    /// Subscribes to the agent's <c>Agent.Started</c> signal synchronously and returns a Task
    /// that completes when the signal arrives (or the timeout elapses). Callers may invoke
    /// this before seeding the agent file so the subscription is in place no matter how soon
    /// the supervisor reconciles. The returned Task drives ticks internally until the signal
    /// is received.
    /// </summary>
    public async ValueTask WaitForAgentAsync(string agentId, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        var bus = Services.GetRequiredService<IEventBus>();

        if (AgentAlreadyStarted(agentId))
        {
            return;
        }
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = bus.Subscribe<AgentLifecycleEvent>(
            $"{Event.WellKnown.Agent.Started}:+",
            EventDeliveryMode.Awaited,
            (envelope, _) =>
            {
                if (SessionId.TryParse(envelope.Type.Id ?? string.Empty, out var session)
                    && string.Equals(session.AgentId, agentId, StringComparison.OrdinalIgnoreCase))
                {
                    started.TrySetResult();
                }
                return ValueTask.CompletedTask;
            });

        if (AgentAlreadyStarted(agentId))
        {
            return;
        }

        await WaitForStartedAsync(agentId, timeout ?? TimeSpan.FromSeconds(5), started, subscription);
    }

    private bool AgentAlreadyStarted(string agentId)
    {
        var repository = Services.GetRequiredService<IAgentInstanceRepository>();
        return repository.GetAllAgents().Any(h =>
            h.SessionPath.IsRootSession
            && h.Started
            && string.Equals(h.SessionPath.Current.AgentId, agentId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task WaitForStartedAsync(string agentId, TimeSpan timeout, TaskCompletionSource started, IDisposable subscription)
    {
        using var _ = subscription;
        using var cts = new CancellationTokenSource(timeout);
        var pollTask = PollTicksAsync(started.Task, cts.Token);
        try
        {
            await started.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException($"Agent '{agentId}' did not start within the timeout.", ex);
        }
        finally
        {
            try { await pollTask; }
            catch { /* poll task exits on cancellation or signal */ }
        }
    }

    private async Task PollTicksAsync(Task started, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !started.IsCompleted)
        {
            try
            {
                await TickAsync(cancellationToken);
            }
            catch (OperationCanceledException) { return; }
            if (started.IsCompleted) return;
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }
            catch (OperationCanceledException) { return; }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseStaticWebAssets();
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Paths:DataRoot"] = DataRoot,
                ["Paths:AgentsRoot"] = AgentsRoot,
                ["Paths:WorkspaceRoot"] = WorkspaceRoot,
                ["Paths:TemplatesRoot"] = TemplatesRoot,
            });
        });
        builder.ConfigureTestServices(services =>
        {
            for (var i = services.Count - 1; i >= 0; i--)
            {
                var t = services[i].ServiceType;
                if (t == typeof(IProviderFactory) || t == typeof(IEmbeddingProviderFactory))
                {
                    services.RemoveAt(i);
                }
            }
            services.AddSingleton<IProviderFactory>(ProviderFactory);
            services.AddSingleton<IEmbeddingProviderFactory, StubEmbeddingProviderFactory>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            TryDeleteDirectory(DataRoot);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
