using System.Text.Json;
using LlamaShears.Agent.Abstractions.Events;
using LlamaShears.Agent.Abstractions.Persistence;
using LlamaShears.Api.Web.Services;
using LlamaShears.Hosting;
using LlamaShears.IntegrationTests.Hosting;
using LlamaShears.Provider.Abstractions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.IntegrationTests;

public sealed class ContextPersistenceTests
{
    private const string AgentId = "alpha";
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(5);

    [Test]
    public async Task UserMessageIsPersistedToCurrentJsonAfterTheAgentResponds()
    {
        await using var factory = new IsolatedAppFactory();
        factory.SeedAgent(AgentId, """
            { "model": { "id": "TEST/dummy" } }
            """);
        using var client = factory.CreateClient();
        await factory.WaitForAgentAsync(AgentId);

        await SendUserMessageAndWaitForReplyAsync(factory, "hello");

        var contextPath = ContextPathFor(factory, AgentId);
        await Assert.That(File.Exists(contextPath)).IsTrue();

        var entries = await ReadContextEntriesAsync(contextPath);
        await Assert.That(entries).Contains(e =>
            e.GetProperty("kind").GetString() == "turn"
            && e.GetProperty("role").GetString() == "User"
            && e.GetProperty("content").GetString() == "hello");
        await Assert.That(entries).Contains(e =>
            e.GetProperty("role").GetString() == "Assistant");
    }

    [Test]
    public async Task AgentDirectoryReturnsPersistedTurnsForUiHistorySeed()
    {
        await using var factory = new IsolatedAppFactory();
        factory.SeedAgent(AgentId, """
            { "model": { "id": "TEST/dummy" } }
            """);
        using var client = factory.CreateClient();
        await factory.WaitForAgentAsync(AgentId);
        await SendUserMessageAndWaitForReplyAsync(factory, "remember me");

        var directory = factory.Services.GetRequiredService<IAgentDirectory>();
        var turns = await directory.GetTurnsAsync(AgentId, CancellationToken.None);

        await Assert.That(turns).Contains(t => t.Role == ModelRole.User && t.Content == "remember me");
        await Assert.That(turns).Contains(t => t.Role == ModelRole.Assistant);
    }

    [Test]
    public async Task ExternalClearWithArchiveLeavesAgentFolderAndEmptiesAgentContext()
    {
        await using var factory = new IsolatedAppFactory();
        factory.SeedAgent(AgentId, """
            { "model": { "id": "TEST/dummy" } }
            """);
        using var client = factory.CreateClient();
        await factory.WaitForAgentAsync(AgentId);
        await SendUserMessageAndWaitForReplyAsync(factory, "hello");

        var store = factory.Services.GetRequiredService<IContextStore>();
        await store.ClearAsync(AgentId, archive: true, CancellationToken.None);

        var folder = Path.Combine(factory.DataRoot, "context", AgentId);
        var contextPath = ContextPathFor(factory, AgentId);

        await Assert.That(Directory.Exists(folder)).IsTrue();
        await Assert.That(File.Exists(contextPath)).IsFalse();

        var archives = Directory.EnumerateFiles(folder, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != "current")
            .Where(name => long.TryParse(name, out _))
            .ToList();
        await Assert.That(archives).IsNotEmpty();

        var liveContext = await store.OpenAsync(AgentId, CancellationToken.None);
        await Assert.That(liveContext.Turns.Count).IsEqualTo(0);
    }

    private static async Task SendUserMessageAndWaitForReplyAsync(
        IsolatedAppFactory factory,
        string content)
    {
        var publisher = factory.Services.GetRequiredService<IAsyncPublisher<UserMessageSubmitted>>();
        var subscriber = factory.Services.GetRequiredService<IAsyncSubscriber<AgentTurnEmitted>>();

        var done = new TaskCompletionSource<AgentTurnEmitted>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = subscriber.Subscribe((evt, _) =>
        {
            if (evt.AgentId == AgentId && evt.Turn.Role == ModelRole.Assistant)
            {
                done.TrySetResult(evt);
            }
            return ValueTask.CompletedTask;
        });

        await publisher.PublishAsync(
            new UserMessageSubmitted(AgentId, content, DateTimeOffset.UtcNow),
            CancellationToken.None);

        var winner = await Task.WhenAny(done.Task, Task.Delay(ResponseTimeout));
        if (winner != done.Task)
        {
            throw new TimeoutException($"Agent '{AgentId}' did not reply within {ResponseTimeout}.");
        }
    }

    private static string ContextPathFor(IsolatedAppFactory factory, string agentId)
    {
        var paths = factory.Services.GetRequiredService<IShearsPaths>();
        var folder = paths.GetPath(PathKind.Context, agentId);
        return Path.Combine(folder, "current.json");
    }

    private static async Task<IReadOnlyList<JsonElement>> ReadContextEntriesAsync(string path)
    {
        var lines = await File.ReadAllLinesAsync(path, CancellationToken.None);
        return [.. lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonDocument.Parse(line).RootElement.Clone())];
    }
}
