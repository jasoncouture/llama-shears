using System.Collections.Concurrent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Agent.Core;

internal sealed class FakeContextStore : IContextStore
{
    private readonly ConcurrentDictionary<SessionId, IAgentContext> _contexts = new();

    public FakeContextStore With(SessionId session, IAgentContext context)
    {
        _contexts[session] = context;
        return this;
    }

    public Task<IAgentContext> OpenAsync(SessionId session, CancellationToken cancellationToken)
        => Task.FromResult(_contexts.GetOrAdd(session, key => new FakeAgentContext(key.AgentId)));

    public IAsyncEnumerable<IContextEntry> ReadCurrentAsync(SessionId session, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public IAsyncEnumerable<IContextEntry> ReadArchiveAsync(ArchiveId archiveId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<string>> ListAgentsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>([.. _contexts.Keys.Select(k => k.AgentId).Distinct(StringComparer.Ordinal)]);

    public Task<IReadOnlyList<ArchiveId>> ListArchivesAsync(SessionId session, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task ClearAsync(SessionId session, bool archive, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task DeleteAsync(ArchiveId archiveId, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
