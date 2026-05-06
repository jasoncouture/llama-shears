using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Abstractions.Persistence;

public interface IAgentContext
{
    string AgentId { get; }

    IReadOnlyList<ModelTurn> Turns { get; }

    IReadOnlyList<IContextEntry> Entries { get; }

    Task AppendAsync(IContextEntry entry, CancellationToken cancellationToken);

    event EventHandler? Cleared;
}
