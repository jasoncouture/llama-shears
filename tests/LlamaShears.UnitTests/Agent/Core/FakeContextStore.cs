using System.Collections.Concurrent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Agent.Core;

internal sealed class FakeContextStore : IContextStore
{
    private readonly ConcurrentDictionary<string, IAgentContext> _contexts = new(StringComparer.Ordinal);

    public FakeContextStore With(string agentId, IAgentContext context)
    {
        _contexts[agentId] = context;
        return this;
    }

    public Task<IAgentContext> OpenAsync(string agentId, CancellationToken cancellationToken)
        => Task.FromResult(_contexts.GetOrAdd(agentId, id => new FakeAgentContext(id)));

    public IAsyncEnumerable<IContextEntry> ReadCurrentAsync(string agentId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public IAsyncEnumerable<IContextEntry> ReadArchiveAsync(ArchiveId archiveId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<string>> ListAgentsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>([.. _contexts.Keys]);

    public Task<IReadOnlyList<ArchiveId>> ListArchivesAsync(string agentId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task ClearAsync(string agentId, bool archive, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task DeleteAsync(ArchiveId archiveId, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
