using Cronos;
using LlamaShears.Core.Abstractions.Agent;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Cron;

public sealed partial class CronScheduler : ICronScheduler
{
    private readonly ICronStore _store;
    private readonly IAgentManager _agents;
    private readonly TimeProvider _time;
    private readonly ILogger<CronScheduler> _logger;

    public CronScheduler(ICronStore store, IAgentManager agents, TimeProvider time, ILogger<CronScheduler> logger)
    {
        _store = store;
        _agents = agents;
        _time = time;
        _logger = logger;
    }

    public async ValueTask<CronJob> ScheduleAsync(
        string agentId,
        string name,
        string cronExpression,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var parsed = ParseOrThrow(cronExpression);
        var now = _time.GetUtcNow();
        var nextFireAt = parsed.GetNextOccurrence(now, TimeZoneInfo.Utc);

        var job = new CronJob(
            Id: Guid.NewGuid(),
            AgentId: agentId,
            Name: name,
            CronExpression: cronExpression,
            Prompt: prompt,
            CreatedAt: now)
        {
            NextFireAt = nextFireAt,
        };

        await _store.UpsertAsync(job, cancellationToken).ConfigureAwait(false);
        LogScheduled(_logger, agentId, job.Id, name, cronExpression);
        return job;
    }

    public async ValueTask<IReadOnlyList<CronJob>> ListByAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        var all = await _store.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return [.. all.Where(j => string.Equals(j.AgentId, agentId, StringComparison.Ordinal))];
    }

    public async ValueTask<bool> CancelAsync(string agentId, Guid id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        var existing = await _store.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null || !string.Equals(existing.AgentId, agentId, StringComparison.Ordinal))
        {
            return false;
        }

        var removed = await _store.RemoveAsync(id, cancellationToken).ConfigureAwait(false);
        if (removed)
        {
            LogCancelled(_logger, agentId, id, existing.Name);
        }
        return removed;
    }

    public async ValueTask<CronJob?> EditAsync(
        string agentId,
        Guid id,
        CronJobEdit edit,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentNullException.ThrowIfNull(edit);
        // Reject whitespace-only fields up front. Schedule rejects blank
        // values; Edit must be the same — otherwise an edit can leave a
        // job persisted with a blank name/prompt/expression that
        // schedule would never have allowed in the first place.
        if (edit.Name is not null && string.IsNullOrWhiteSpace(edit.Name))
        {
            throw new ArgumentException("Cron job name must not be blank.", nameof(edit));
        }
        if (edit.Prompt is not null && string.IsNullOrWhiteSpace(edit.Prompt))
        {
            throw new ArgumentException("Cron job prompt must not be blank.", nameof(edit));
        }
        if (edit.CronExpression is not null && string.IsNullOrWhiteSpace(edit.CronExpression))
        {
            throw new ArgumentException("Cron expression must not be blank.", nameof(edit));
        }

        var existing = await _store.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null || !string.Equals(existing.AgentId, agentId, StringComparison.Ordinal))
        {
            return null;
        }

        var newName = edit.Name ?? existing.Name;
        var newPrompt = edit.Prompt ?? existing.Prompt;
        var newEnabled = edit.Enabled ?? existing.Enabled;
        var newExpression = edit.CronExpression ?? existing.CronExpression;

        var nextFireAt = existing.NextFireAt;
        if (!string.Equals(newExpression, existing.CronExpression, StringComparison.Ordinal))
        {
            var parsed = ParseOrThrow(newExpression);
            nextFireAt = parsed.GetNextOccurrence(_time.GetUtcNow(), TimeZoneInfo.Utc);
        }

        var updated = existing with
        {
            Name = newName,
            CronExpression = newExpression,
            Prompt = newPrompt,
            Enabled = newEnabled,
            NextFireAt = nextFireAt,
        };

        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
        LogEdited(_logger, agentId, id, newName);
        return updated;
    }

    public async ValueTask<bool> TriggerAsync(string agentId, Guid id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        var existing = await _store.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null || !string.Equals(existing.AgentId, agentId, StringComparison.Ordinal))
        {
            return false;
        }

        await FireSingleAsync(existing, _time.GetUtcNow(), manual: true, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async ValueTask FireDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var jobs = await _store.GetAllAsync(cancellationToken).ConfigureAwait(false);
        foreach (var job in jobs)
        {
            if (!job.Enabled)
            {
                continue;
            }
            if (job.NextFireAt is null || now < job.NextFireAt.Value)
            {
                continue;
            }
            if (!_agents.Contains(job.AgentId))
            {
                LogSkippedMissingAgent(_logger, job.Id, job.AgentId);
                continue;
            }

            try
            {
                await FireSingleAsync(job, now, manual: false, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One bad job (e.g. invalid expression from a manual
                // file edit) must not stop the rest of the tick.
                LogFireFailed(_logger, job.Id, job.AgentId, ex);
            }
        }
    }

    private async Task FireSingleAsync(CronJob job, DateTimeOffset firedAt, bool manual, CancellationToken cancellationToken)
    {
        // STUB: log the would-have-been agent input. When the executor
        // graduates from stub to real, this is where the channel publish
        // (or whatever execution surface lands) goes. The prompt body is
        // intentionally not logged — it is user/agent-provided text that
        // can carry secrets; only the length is recorded.
        LogStubFire(_logger, job.Id, job.AgentId, manual, job.Prompt.Length);

        var parsed = ParseOrThrow(job.CronExpression);
        var nextFireAt = parsed.GetNextOccurrence(firedAt, TimeZoneInfo.Utc);
        var updated = job with
        {
            LastFiredAt = firedAt,
            NextFireAt = nextFireAt,
        };
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    private static CronExpression ParseOrThrow(string expression)
    {
        try
        {
            return CronExpression.Parse(expression);
        }
        catch (CronFormatException ex)
        {
            throw new ArgumentException(
                $"Cron expression '{expression}' is not parseable: {ex.Message}",
                nameof(expression),
                ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' scheduled cron job '{JobId}' '{Name}' (expression '{Expression}').")]
    private static partial void LogScheduled(ILogger logger, string agentId, Guid jobId, string name, string expression);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' cancelled cron job '{JobId}' '{Name}'.")]
    private static partial void LogCancelled(ILogger logger, string agentId, Guid jobId, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' edited cron job '{JobId}' '{Name}'.")]
    private static partial void LogEdited(ILogger logger, string agentId, Guid jobId, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "[cron stub] Job '{JobId}' for agent '{AgentId}' (manual={Manual}) would fire with a prompt of length {PromptLength}.")]
    private static partial void LogStubFire(ILogger logger, Guid jobId, string agentId, bool manual, int promptLength);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cron job '{JobId}' due but agent '{AgentId}' is not currently loaded; skipping.")]
    private static partial void LogSkippedMissingAgent(ILogger logger, Guid jobId, string agentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cron job '{JobId}' for agent '{AgentId}' failed to fire on this tick; remaining jobs continue.")]
    private static partial void LogFireFailed(ILogger logger, Guid jobId, string agentId, Exception ex);
}
