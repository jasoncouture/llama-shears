using System.Text;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Persistence;

internal sealed class AgentContext : IAgentContext
{
    private readonly Lock _lock = new Lock();
    private readonly List<IContextEntry> _entries;
    private readonly string _currentPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AgentContext(
        SessionId session,
        string currentPath,
        IEnumerable<IContextEntry> seed,
        JsonSerializerOptions jsonOptions)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPath);
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(jsonOptions);

        Session = session;
        _currentPath = currentPath;
        _entries = [.. seed];
        _jsonOptions = jsonOptions;
    }

    public SessionId Session { get; }

    public string AgentId => Session.AgentId;

    public Guid? SessionId => Session.IsDefault ? null : Session.Id;

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

    public event EventHandler<IContextEntry>? Appended;

    public async Task AppendAsync(IContextEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var line = JsonSerializer.Serialize(entry, _jsonOptions) + "\n";
        await File.AppendAllTextAsync(_currentPath, line, Encoding.UTF8, cancellationToken);

        lock (_lock)
        {
            _entries.Add(entry);
        }
        Appended?.Invoke(this, entry);
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
