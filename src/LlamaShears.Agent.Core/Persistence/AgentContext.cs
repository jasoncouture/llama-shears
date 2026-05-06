using System.Text;
using System.Text.Json;
using LlamaShears.Agent.Abstractions.Persistence;
using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Core.Persistence;

internal sealed class AgentContext : IAgentContext
{
    private readonly Lock _lock = new();
    private readonly List<IContextEntry> _entries;
    private readonly string _currentPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AgentContext(
        string agentId,
        string currentPath,
        IEnumerable<IContextEntry> seed,
        JsonSerializerOptions jsonOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPath);
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(jsonOptions);

        AgentId = agentId;
        _currentPath = currentPath;
        _entries = [.. seed];
        _jsonOptions = jsonOptions;
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

    public async Task AppendAsync(IContextEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var line = JsonSerializer.Serialize(entry, _jsonOptions) + "\n";
        await File.AppendAllTextAsync(_currentPath, line, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        lock (_lock)
        {
            _entries.Add(entry);
        }
    }

    internal void ClearInMemory()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
        Cleared?.Invoke(this, EventArgs.Empty);
    }
}
