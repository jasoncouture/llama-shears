using System.Collections.Concurrent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Agent.Core;

internal sealed class FakeContextStore : IContextStore
{
    private readonly ConcurrentDictionary<(string AgentId, Guid? SessionId), IAgentContext> _contexts = new();

    public FakeContextStore With(string agentId, IAgentContext context)
    {
        _contexts[(agentId, null)] = context;
        return this;
    }

    public FakeContextStore With(string agentId, Guid? sessionId, IAgentContext context)
    {
        _contexts[(agentId, sessionId)] = context;
        return this;
    }

    public Task<IAgentContext> OpenAsync(string agentId, Guid? sessionId, CancellationToken cancellationToken)
        => Task.FromResult(_contexts.GetOrAdd((agentId, sessionId), key => new FakeAgentContext(key.AgentId)));

    public IAsyncEnumerable<IContextEntry> ReadCurrentAsync(string agentId, Guid? sessionId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public IAsyncEnumerable<IContextEntry> ReadArchiveAsync(ArchiveId archiveId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<string>> ListAgentsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>([.. _contexts.Keys.Select(k => k.AgentId).Distinct(StringComparer.Ordinal)]);

    public Task<IReadOnlyList<ArchiveId>> ListArchivesAsync(string agentId, Guid? sessionId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task ClearAsync(string agentId, Guid? sessionId, bool archive, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task DeleteAsync(ArchiveId archiveId, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
