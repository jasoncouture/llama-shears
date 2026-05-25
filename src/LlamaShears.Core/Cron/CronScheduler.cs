using Cronos;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Cron;

public sealed partial class CronScheduler : ICronScheduler
{
    private const string CronChannelName = "cron";

    private readonly ICronStore _store;
    private readonly IDataContextScope _scope;
    private readonly IPromptedAgentSpawner _spawner;
    private readonly TimeProvider _time;
    private readonly ILogger<CronScheduler> _logger;

    public CronScheduler(
        ICronStore store,
        IDataContextScope scope,
        IPromptedAgentSpawner spawner,
        TimeProvider time,
        ILogger<CronScheduler> logger)
    {
        _store = store;
        _scope = scope;
        _spawner = spawner;
        _time = time;
        _logger = logger;
    }

    private string AgentId => _scope.GetAgentConfig().Id;

    public async ValueTask<CronJob> ScheduleAsync(
        string name,
        string cronExpression,
        string prompt,
        bool oneShot = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var agentId = AgentId;
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
            OneShot = oneShot,
        };

        await _store.UpsertAsync(job, cancellationToken);
        LogScheduled(agentId, job.Id, name, cronExpression);
        return job;
    }

    public async ValueTask<IReadOnlyList<CronJob>> ListAsync(CancellationToken cancellationToken = default)
        => await _store.GetAllAsync(AgentId, cancellationToken);

    public async ValueTask<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agentId = AgentId;
        var existing = await _store.GetAsync(agentId, id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var removed = await _store.RemoveAsync(agentId, id, cancellationToken);
        if (removed)
        {
            LogCancelled(agentId, id, existing.Name);
        }
        return removed;
    }

    public async ValueTask<CronJob?> EditAsync(
        Guid id,
        CronJobEdit edit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(edit);
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

        var agentId = AgentId;
        var existing = await _store.GetAsync(agentId, id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var newName = edit.Name ?? existing.Name;
        var newPrompt = edit.Prompt ?? existing.Prompt;
        var newEnabled = edit.Enabled ?? existing.Enabled;
        var newOneShot = edit.OneShot ?? existing.OneShot;
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
            OneShot = newOneShot,
            NextFireAt = nextFireAt,
        };

        await _store.UpsertAsync(updated, cancellationToken);
        LogEdited(agentId, id, newName);
        return updated;
    }

    public async ValueTask<bool> TriggerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _store.GetAsync(AgentId, id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        await FireSingleAsync(existing, _time.GetUtcNow(), manual: true, cancellationToken);
        return true;
    }

    public async ValueTask FireDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var jobs = await _store.GetAllAsync(AgentId, cancellationToken);
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

            try
            {
                await FireSingleAsync(job, now, manual: false, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogFireFailed(job.Id, job.AgentId, ex);
            }
        }
    }

    private async Task FireSingleAsync(CronJob job, DateTimeOffset firedAt, bool manual, CancellationToken cancellationToken)
    {
        var agentConfig = _scope.GetAgentConfig();
        var parentPath = _scope.GetSessionPath();
        var subAgentConfig = PromptedAgentStartInformation.CreateDefaultSubAgentConfig(CronChannelName, agentConfig);
        var childSession = SessionId.CreateFor(agentConfig.Id, $"{CronChannelName}-{job.Id:n}");
        var initialPrompt = new ModelTurn(
            ModelRole.User,
            job.Prompt,
            firedAt,
            $"subagent:{CronChannelName}");
        var startInfo = new PromptedAgentStartInformation(
            Config: subAgentConfig,
            Id: childSession,
            ParentSessionPath: parentPath,
            InitialPrompt: initialPrompt);

        await _spawner.CreateAsync(startInfo, cancellationToken);
        LogFired(job.Id, job.AgentId, manual, job.Name);

        var parsed = ParseOrThrow(job.CronExpression);
        var nextFireAt = parsed.GetNextOccurrence(firedAt, TimeZoneInfo.Utc);
        var updated = job with
        {
            LastFiredAt = firedAt,
            NextFireAt = nextFireAt,
            Enabled = job.Enabled && !job.OneShot,
        };
        if (job.OneShot)
        {
            LogOneShotDisabled(job.Id, job.AgentId, job.Name);
        }
        await _store.UpsertAsync(updated, cancellationToken);
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
    private partial void LogScheduled(string agentId, Guid jobId, string name, string expression);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' cancelled cron job '{JobId}' '{Name}'.")]
    private partial void LogCancelled(string agentId, Guid jobId, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' edited cron job '{JobId}' '{Name}'.")]
    private partial void LogEdited(string agentId, Guid jobId, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cron job '{JobId}' '{Name}' fired for agent '{AgentId}' (manual={Manual}); transient sub-agent launched.")]
    private partial void LogFired(Guid jobId, string agentId, bool manual, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cron job '{JobId}' '{Name}' for agent '{AgentId}' was one-shot; auto-disabled after fire.")]
    private partial void LogOneShotDisabled(Guid jobId, string agentId, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cron job '{JobId}' for agent '{AgentId}' failed to fire on this tick; remaining jobs continue.")]
    private partial void LogFireFailed(Guid jobId, string agentId, Exception ex);
}
