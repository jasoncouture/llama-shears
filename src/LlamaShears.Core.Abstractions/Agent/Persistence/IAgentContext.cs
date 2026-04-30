using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent.Persistence;

public interface IAgentContext
{
    string AgentId { get; }

    IReadOnlyList<ModelTurn> Turns { get; }

    IReadOnlyList<IContextEntry> Entries { get; }

    Task AppendAsync(IContextEntry entry, CancellationToken cancellationToken);

    event EventHandler? Cleared;
}
