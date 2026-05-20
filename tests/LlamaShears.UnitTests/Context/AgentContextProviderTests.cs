using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Context;
using LlamaShears.UnitTests.Agent.Core;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Context;

public sealed class AgentContextProviderTests
{
    [Test]
    public async Task CreateAgentContextWithIdReturnsSnapshotForKnownAgent()
    {
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.FromMinutes(5));
        var configProvider = new StubAgentConfigProvider { Configs = { ["alpha"] = config } };
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 4, 30, 12, 0, 0, TimeSpan.Zero));
        var turn = new ModelTurn(ModelRole.User, "hi", time.GetUtcNow());
        var session = new SessionId("alpha", SessionId.DefaultSessionName);
        var contextStore = new StubContextStore { Contexts = { ["alpha"] = new StubAgentContext("alpha", [turn]) } };
        var provider = new AgentContextProvider(configProvider, contextStore, time);

        var context = await provider.CreateAgentContextAsync(session, CancellationToken.None);

        await Assert.That(context).IsNotNull();
        await Assert.That(context!.AgentId).IsEqualTo("alpha");
        await Assert.That(context.Now).IsEqualTo(time.GetUtcNow());
        await Assert.That(context.Config).IsSameReferenceAs(config);
        await Assert.That(context.LanguageModel.Turns.Length).IsEqualTo(1);
        await Assert.That(context.LanguageModel.Turns[0]).IsSameReferenceAs(turn);
        await Assert.That(context.LanguageModel.Entries.Length).IsEqualTo(1);
        await Assert.That(context.LanguageModel.Entries[0]).IsSameReferenceAs(turn);
        await Assert.That(context.Tools.Items.Length).IsEqualTo(0);
        await Assert.That(context.Plugins.Data.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CreateAgentContextWithIdReturnsNullForUnknownAgent()
    {
        var configProvider = new StubAgentConfigProvider();
        var provider = new AgentContextProvider(configProvider, new StubContextStore(), new FakeTimeProvider());

        var context = await provider.CreateAgentContextAsync(
            new SessionId("missing", SessionId.DefaultSessionName),
            CancellationToken.None);

        await Assert.That(context).IsNull();
    }

    [Test]
    public async Task ParameterlessCreateAgentContextThrowsNotImplemented()
    {
        var provider = new AgentContextProvider(
            new StubAgentConfigProvider(),
            new StubContextStore(),
            new FakeTimeProvider());

        await Assert.That(() => provider.CreateAgentContextAsync(CancellationToken.None).AsTask())
            .Throws<NotImplementedException>();
    }

    private sealed class StubAgentConfigProvider : IAgentConfigProvider
    {
        public Dictionary<string, AgentConfig> Configs { get; } =
            new Dictionary<string, AgentConfig>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> ListAgentIds() => [.. Configs.Keys];

        public ValueTask<AgentConfig?> GetConfigAsync(string agentId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Configs.TryGetValue(agentId, out var config) ? config : null);

        public ValueTask<AgentConfigFile?> ReadFileAsync(string agentId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<SaveAgentConfigResult> SaveAsync(
            string agentId,
            string expectedHash,
            string content,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubContextStore : IContextStore
    {
        public Dictionary<string, IAgentContext> Contexts { get; } =
            new Dictionary<string, IAgentContext>(StringComparer.Ordinal);

        public Task<IAgentContext> OpenAsync(SessionId session, CancellationToken cancellationToken)
        {
            if (!Contexts.TryGetValue(session.AgentId, out var context))
            {
                context = new StubAgentContext(session.AgentId, []);
                Contexts[session.AgentId] = context;
            }
            return Task.FromResult(context);
        }

        public IAsyncEnumerable<IContextEntry> ReadCurrentAsync(SessionId session, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<IContextEntry> ReadArchiveAsync(ArchiveId archiveId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<string>> ListAgentsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ArchiveId>> ListArchivesAsync(SessionId session, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ClearAsync(SessionId session, bool archive, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(ArchiveId archiveId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubAgentContext : IAgentContext
    {
        private readonly List<IContextEntry> _entries;

        public StubAgentContext(string agentId, IEnumerable<IContextEntry> entries)
        {
            AgentId = agentId;
            _entries = [.. entries];
        }

        public string AgentId { get; }

        public IReadOnlyList<ModelTurn> Turns => [.. _entries.OfType<ModelTurn>()];

        public IReadOnlyList<IContextEntry> Entries => [.. _entries];

        public event EventHandler? Cleared;

#pragma warning disable CS0067 // Stub does not raise Appended.
        public event EventHandler<IContextEntry>? Appended;
#pragma warning restore CS0067

        public Task AppendAsync(IContextEntry entry, CancellationToken cancellationToken)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        internal void RaiseCleared() => Cleared?.Invoke(this, EventArgs.Empty);
    }
}
