using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Agent.Core;

internal sealed class FakeAgentContext : IAgentContext
{
    private readonly Lock _lock = new();
    private readonly List<IContextEntry> _entries = [];

    public FakeAgentContext(string agentId, IEnumerable<IContextEntry>? seed = null)
    {
        AgentId = agentId;
        if (seed is not null)
        {
            _entries.AddRange(seed);
        }
    }

    public string AgentId { get; }

    public IReadOnlyList<ModelTurn> Turns
    {
        get
        {
            lock (_lock)
            {
                return [.. _entries.OfType<ModelTurn>()];
            }
        }
    }

    public IReadOnlyList<IContextEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return [.. _entries];
            }
        }
    }

    public event EventHandler? Cleared;

    public Task AppendAsync(IContextEntry entry, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _entries.Add(entry);
        }
        return Task.CompletedTask;
    }

    public void RaiseCleared()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
        Cleared?.Invoke(this, EventArgs.Empty);
    }
}
