using LlamaShears.Agent.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Provider;
using MessagePipe;
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
    /// Publishes a system tick so the <see cref="AgentManager"/> rescans
    /// the seeded agents directory immediately. The production tick
    /// service runs on a 30-second interval, which is too slow for tests.
    /// </summary>
    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        var publisher = Services.GetRequiredService<IAsyncPublisher<SystemTick>>();
        await publisher.PublishAsync(new SystemTick(DateTimeOffset.UtcNow), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes a tick and polls until <paramref name="agentId"/> is
    /// present in <see cref="AgentManager.Agents"/>, or the timeout
    /// elapses. The reconcile runs synchronously inside the tick handler,
    /// so a single pulse is normally enough; the poll only exists so a
    /// slow CI host doesn't flake.
    /// </summary>
    public async Task WaitForAgentAsync(string agentId, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        var manager = Services.GetRequiredService<AgentManager>();
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        await TickAsync().ConfigureAwait(false);
        while (!manager.Agents.ContainsKey(agentId))
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Agent '{agentId}' did not appear in AgentManager within the timeout.");
            }
            await Task.Delay(25).ConfigureAwait(false);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // MapStaticAssets reads its manifest from the host project's
        // build output. In a published or `dotnet run` flow the SDK
        // hooks this up automatically; under WebApplicationFactory
        // (Testing environment, non-published) we have to opt in
        // explicitly or `_framework/blazor.web.js` and friends 404.
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
            // Strip every real IProviderFactory the host registered (Ollama,
            // future cloud providers) and substitute the in-process stub.
            // No matter what an agent JSON references, the resolved model
            // is StubLanguageModel — live network calls are structurally
            // impossible under test.
            for (var i = services.Count - 1; i >= 0; i--)
            {
                if (services[i].ServiceType == typeof(IProviderFactory))
                {
                    services.RemoveAt(i);
                }
            }
            services.AddSingleton<IProviderFactory>(ProviderFactory);
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
            // Best-effort cleanup; ignore tail-end file locks.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
