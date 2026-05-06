using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Abstractions.Persistence;

public interface IConversationStore
{
    Task<IAgentContext> OpenAsync(string agentId, CancellationToken cancellationToken);

    IAsyncEnumerable<IConversationEntry> ReadCurrentAsync(string agentId, CancellationToken cancellationToken);

    IAsyncEnumerable<IConversationEntry> ReadArchiveAsync(ArchiveId archiveId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListAgentsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ArchiveId>> ListArchivesAsync(string agentId, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the agent's conversation. With <paramref name="archive"/>=true,
    /// renames <c>current.json</c> to <c>&lt;UnixMillis&gt;.json</c>; otherwise
    /// deletes <c>current.json</c>. The agent's folder is never removed by the
    /// framework — that is the user's or a plugin's job.
    /// </summary>
    Task ClearAsync(string agentId, bool archive, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a single archive file identified by <paramref name="archiveId"/>.
    /// Does not touch <c>current.json</c>, other archives, or the agent folder.
    /// </summary>
    Task DeleteAsync(ArchiveId archiveId, CancellationToken cancellationToken);
}
