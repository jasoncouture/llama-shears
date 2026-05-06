using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Abstractions.Persistence;

public interface IAgentContext
{
    string AgentId { get; }

    IReadOnlyList<ModelTurn> Turns { get; }

    IReadOnlyList<IConversationEntry> Entries { get; }

    Task AppendAsync(IConversationEntry entry, CancellationToken cancellationToken);

    event EventHandler? Cleared;
}
