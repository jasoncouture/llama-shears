namespace LlamaShears.Core.Cron;

/// <summary>
/// Agent-scoped cron operations. The scheduler reads the calling agent from
/// the ambient data-context scope; callers must already be inside an agent
/// scope. Every operation only touches that agent's jobs — there is no way
/// to read or mutate another agent's state through this interface.
/// </summary>
public interface ICronScheduler
{
    /// <summary>
    /// Creates a new job for the current agent. Throws when
    /// <paramref name="cronExpression"/> is unparseable.
    /// </summary>
    ValueTask<CronJob> ScheduleAsync(
        string name,
        string cronExpression,
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>Returns every job owned by the current agent.</summary>
    ValueTask<IReadOnlyList<CronJob>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a job. Returns <see langword="false"/> when the id is unknown
    /// or belongs to a different agent.
    /// </summary>
    ValueTask<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a patch to a job, recomputing <see cref="CronJob.NextFireAt"/>
    /// when <see cref="CronJobEdit.CronExpression"/> changes. Returns the
    /// updated job, or <see langword="null"/> when the id is unknown or
    /// belongs to a different agent.
    /// </summary>
    ValueTask<CronJob?> EditAsync(
        Guid id,
        CronJobEdit edit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces an immediate fire of the job (the stub log + NextFireAt
    /// recomputation). Returns <see langword="false"/> when the id is
    /// unknown or belongs to a different agent.
    /// </summary>
    ValueTask<bool> TriggerAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fires every enabled job owned by the current agent whose
    /// <see cref="CronJob.NextFireAt"/> is at or before <paramref name="now"/>.
    /// Called by the per-agent cron service on each tick of the default
    /// session; never directly from MCP tools.
    /// </summary>
    ValueTask FireDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
