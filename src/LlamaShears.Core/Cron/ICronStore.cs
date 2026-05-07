namespace LlamaShears.Core.Cron;

/// <summary>
/// Persistence layer for cron jobs. Single global store; agent scoping
/// is the scheduler's responsibility, not the store's.
/// </summary>
public interface ICronStore
{
    /// <summary>Returns every persisted job, ungated by agent.</summary>
    ValueTask<IReadOnlyList<CronJob>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a single job by id, or <see langword="null"/> when absent.</summary>
    ValueTask<CronJob?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates or replaces a job. Persists immediately.</summary>
    ValueTask UpsertAsync(CronJob job, CancellationToken cancellationToken = default);

    /// <summary>Deletes a job by id. Returns <see langword="false"/> when no job had that id.</summary>
    ValueTask<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
