namespace LlamaShears.Core.Cron;

/// <summary>
/// Persistence layer for cron jobs. Storage is partitioned per agent — every
/// query takes an <c>agentId</c> and only ever sees that agent's jobs. There
/// is no shared/global view; cross-agent isolation is the store's
/// responsibility, not the scheduler's.
/// </summary>
public interface ICronStore
{
    /// <summary>Returns every job owned by <paramref name="agentId"/>.</summary>
    ValueTask<IReadOnlyList<CronJob>> GetAllAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the job with the given <paramref name="id"/> when it belongs
    /// to <paramref name="agentId"/>; <see langword="null"/> when the job is
    /// missing or owned by a different agent.
    /// </summary>
    ValueTask<CronJob?> GetAsync(string agentId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates or replaces a job under <see cref="CronJob.AgentId"/>. Persists immediately.</summary>
    ValueTask UpsertAsync(CronJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a job. Returns <see langword="false"/> when no job had that id
    /// under <paramref name="agentId"/> (including the case where it exists
    /// but belongs to a different agent).
    /// </summary>
    ValueTask<bool> RemoveAsync(string agentId, Guid id, CancellationToken cancellationToken = default);
}
