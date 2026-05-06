using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed class CurrentAgentAccessor : ICurrentAgentAccessor
{
    private static readonly AsyncLocal<AgentInfo?> _current = new();

    public AgentInfo? Current => _current.Value;

    public IDisposable BeginScope(AgentInfo agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        var previous = _current.Value;
        _current.Value = agent;
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly AgentInfo? _previous;
        private bool _disposed;

        public Scope(AgentInfo? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _current.Value = _previous;
        }
    }
}
