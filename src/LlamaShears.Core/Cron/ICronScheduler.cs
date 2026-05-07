namespace LlamaShears.Core.Cron;

/// <summary>
/// Agent-scoped cron operations. The scheduler is the only surface MCP
/// tools should hit: it validates the cron expression, computes
/// <see cref="CronJob.NextFireAt"/>, and refuses cross-agent reads or
/// mutations. The store sits behind it.
/// </summary>
public interface ICronScheduler
{
    /// <summary>
    /// Creates a new job for <paramref name="agentId"/>. Throws when
    /// <paramref name="cronExpression"/> is unparseable.
    /// </summary>
    ValueTask<CronJob> ScheduleAsync(
        string agentId,
        string name,
        string cronExpression,
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>Returns every job owned by <paramref name="agentId"/>.</summary>
    ValueTask<IReadOnlyList<CronJob>> ListByAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a job. Returns <see langword="false"/> when the id is unknown
    /// or the job belongs to a different agent.
    /// </summary>
    ValueTask<bool> CancelAsync(string agentId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a patch to a job, recomputing <see cref="CronJob.NextFireAt"/>
    /// when <see cref="CronJobEdit.CronExpression"/> changes. Returns the
    /// updated job, or <see langword="null"/> when the id is unknown or
    /// belongs to a different agent.
    /// </summary>
    ValueTask<CronJob?> EditAsync(
        string agentId,
        Guid id,
        CronJobEdit edit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces an immediate fire of the job (the stub log + NextFireAt
    /// recomputation). Returns <see langword="false"/> when the id is
    /// unknown or belongs to a different agent.
    /// </summary>
    ValueTask<bool> TriggerAsync(string agentId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fires every enabled job whose <see cref="CronJob.NextFireAt"/> is
    /// at or before <paramref name="now"/>. Called by the executor on
    /// each tick; never directly from MCP tools.
    /// </summary>
    ValueTask FireDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
