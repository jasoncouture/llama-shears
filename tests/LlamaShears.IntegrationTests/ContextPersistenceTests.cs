using System.Text.Json;
using LlamaShears.Api.Web.Services;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.IntegrationTests.Hosting;
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
    public async Task SlashClearCommandFromChatSessionEmptiesPersistedContext()
    {
        await using var factory = new IsolatedAppFactory();
        factory.SeedAgent(AgentId, """
            { "model": { "id": "TEST/dummy" } }
            """);
        using var client = factory.CreateClient();
        await factory.WaitForAgentAsync(AgentId);
        await SendUserMessageAndWaitForReplyAsync(factory, "hello");

        await using var scope = factory.Services.CreateAsyncScope();
        var session = scope.ServiceProvider.GetRequiredService<IChatSession>();
        await session.SelectAgentAsync(AgentId, CancellationToken.None);
        await session.SendAsync("/clear", CancellationToken.None);

        var contextPath = ContextPathFor(factory, AgentId);
        var folder = Path.Combine(factory.DataRoot, "context", AgentId);

        await Assert.That(File.Exists(contextPath)).IsFalse();
        await Assert.That(Directory.Exists(folder)).IsTrue();
        await Assert.That(session.Bubbles.Count).IsEqualTo(0);

        var store = factory.Services.GetRequiredService<IContextStore>();
        var liveContext = await store.OpenAsync(AgentId, CancellationToken.None);
        await Assert.That(liveContext.Turns.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SlashArchiveCommandFromChatSessionMovesContextToArchive()
    {
        await using var factory = new IsolatedAppFactory();
        factory.SeedAgent(AgentId, """
            { "model": { "id": "TEST/dummy" } }
            """);
        using var client = factory.CreateClient();
        await factory.WaitForAgentAsync(AgentId);
        await SendUserMessageAndWaitForReplyAsync(factory, "hello");

        await using var scope = factory.Services.CreateAsyncScope();
        var session = scope.ServiceProvider.GetRequiredService<IChatSession>();
        await session.SelectAgentAsync(AgentId, CancellationToken.None);
        await session.SendAsync("/archive", CancellationToken.None);

        var folder = Path.Combine(factory.DataRoot, "context", AgentId);
        var contextPath = ContextPathFor(factory, AgentId);

        await Assert.That(File.Exists(contextPath)).IsFalse();

        var archives = Directory.EnumerateFiles(folder, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != "current")
            .Where(name => long.TryParse(name, out _))
            .ToList();
        await Assert.That(archives).IsNotEmpty();
        await Assert.That(session.Bubbles.Count).IsEqualTo(0);
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
        var publisher = factory.Services.GetRequiredService<IEventPublisher>();
        var bus = factory.Services.GetRequiredService<IEventBus>();

        var done = new TaskCompletionSource<ModelTurn>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = bus.Subscribe<ModelTurn>(
            $"{Event.WellKnown.Agent.Turn}:{AgentId}",
            EventDeliveryMode.Awaited,
            (envelope, _) =>
            {
                if (envelope.Data is { Role: ModelRole.Assistant } turn)
                {
                    done.TrySetResult(turn);
                }
                return ValueTask.CompletedTask;
            });

        await publisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = "test" },
            new ChannelMessage(content, AgentId, DateTimeOffset.UtcNow),
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
