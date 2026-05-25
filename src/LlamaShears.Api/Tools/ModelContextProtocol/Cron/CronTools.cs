using System.Collections.Immutable;
using System.ComponentModel;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Cron;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

[McpServerToolType]
public sealed partial class CronTools
{
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ICronScheduler _scheduler;
    private readonly ILogger<CronTools> _logger;

    public CronTools(IAgentWorkspaceLocator workspace, ICronScheduler scheduler, ILogger<CronTools> logger)
    {
        _workspace = workspace;
        _scheduler = scheduler;
        _logger = logger;
    }

    [McpServerTool(Name = "cron_schedule", Destructive = false, OpenWorld = false)]
    [Description("Schedules a recurring task for a transient cron sub-agent of yours. When the job fires, a fresh sub-agent spawns and reads `prompt` as an instruction FROM YOU TO IT — exactly the way the user gives instructions to you. The cron expression is 5-field (minute hour day-of-month month day-of-week) evaluated in UTC. Returns a JSON object with the scheduled flag and the new job's summary.")]
    public async Task<CronScheduleResult> ScheduleCron(
        [Description("Human-readable handle for this job. Used in list output and log messages.")] string name,
        [Description("Cron expression in 5-field form (e.g. '0 9 * * 1-5' for 09:00 UTC weekdays).")] string cronExpression,
        [Description("Instructions you (the scheduling agent) are giving to the future cron sub-agent. Write it as a directive, not as the literal output you want produced. Example: when the user says \"have it say hello in chat every minute\", the prompt is \"Send the message 'hello' to the user's chat channel via the message_send tool.\" — NOT just \"hello\". The sub-agent will read this string as its initial user-role turn and must figure out what tools to call to satisfy it.")] string prompt,
        [Description("When true, the job auto-disables itself after its first successful fire (one-shot). Defaults to false.")] bool oneShot = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return new CronScheduleResult(Scheduled: false, Job: null, Error: "Refused: cron_schedule requires an authenticated agent on the request.");
        }

        try
        {
            var job = await _scheduler.ScheduleAsync(name, cronExpression, prompt, oneShot, cancellationToken);
            return new CronScheduleResult(
                Scheduled: true,
                Job: new CronJobSummary(
                    Id: job.Id,
                    Name: job.Name,
                    CronExpression: job.CronExpression,
                    Prompt: job.Prompt,
                    Enabled: job.Enabled,
                    LastFiredAt: job.LastFiredAt,
                    NextFireAt: job.NextFireAt));
        }
        catch (ArgumentException ex)
        {
            LogScheduleFailed(workspace.AgentId, ex.Message, ex);
            return new CronScheduleResult(Scheduled: false, Job: null, Error: $"Refused: {ex.Message}");
        }
    }

    [McpServerTool(Name = "cron_list", Destructive = false, OpenWorld = false, ReadOnly = true)]
    [Description("Returns the calling agent's cron jobs as a JSON object: jobCount plus an array of jobs (id, name, cronExpression, prompt, enabled, lastFiredAt, nextFireAt). Other agents' jobs are not visible.")]
    public async Task<CronListResult> ListCron(CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return new CronListResult(JobCount: 0, Jobs: [], Error: "Refused: cron_list requires an authenticated agent on the request.");
        }

        var jobs = await _scheduler.ListAsync(cancellationToken);
        var builder = ImmutableArray.CreateBuilder<CronJobSummary>();
        foreach (var job in jobs)
        {
            builder.Add(new CronJobSummary(
                Id: job.Id,
                Name: job.Name,
                CronExpression: job.CronExpression,
                Prompt: job.Prompt,
                Enabled: job.Enabled,
                LastFiredAt: job.LastFiredAt,
                NextFireAt: job.NextFireAt));
        }
        return new CronListResult(JobCount: builder.Count, Jobs: builder.ToImmutable());
    }

    [McpServerTool(Name = "cron_cancel", Idempotent = true, OpenWorld = false)]
    [Description("Cancels a cron job belonging to the calling agent. Returns a JSON object with the parsed jobId and a cancelled flag. Refuses jobs owned by other agents, unknown ids, or unparseable id strings.")]
    public async Task<CronCancelResult> CancelCron(
        [Description("Cron job id (GUID, format-D).")] string id,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return new CronCancelResult(JobId: null, Cancelled: false, Error: "Refused: cron_cancel requires an authenticated agent on the request.");
        }
        if (!Guid.TryParse(id, out var jobId))
        {
            return new CronCancelResult(JobId: null, Cancelled: false, Error: $"Refused: '{id}' is not a valid GUID.");
        }

        var removed = await _scheduler.CancelAsync(jobId, cancellationToken);
        return removed
            ? new CronCancelResult(JobId: jobId, Cancelled: true)
            : new CronCancelResult(JobId: jobId, Cancelled: false, Error: $"No cron job {jobId:D} owned by this agent.");
    }

    [McpServerTool(Name = "cron_edit", Idempotent = true, OpenWorld = false)]
    [Description("Edits a cron job belonging to the calling agent. Any unspecified field is left unchanged. Mutating the cron expression revalidates and recomputes the next fire time. Returns a JSON object with the parsed jobId, an edited flag, and the updated job summary on success.")]
    public async Task<CronEditResult> EditCron(
        [Description("Cron job id (GUID, format-D).")] string id,
        [Description("New human-readable name. Leave null to keep current.")] string? name = null,
        [Description("New 5-field cron expression. Leave null to keep current.")] string? cronExpression = null,
        [Description("New instruction text the future cron sub-agent will execute. Write it as a directive FROM YOU TO THE SUB-AGENT — describe what action to take and which tools to use, not the literal output you want. Example: \"Send 'hello' to the user's chat via message_send\" — NOT just \"hello\". Leave null to keep current.")] string? prompt = null,
        [Description("New enabled flag. Leave null to keep current.")] bool? enabled = null,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return new CronEditResult(JobId: null, Edited: false, Job: null, Error: "Refused: cron_edit requires an authenticated agent on the request.");
        }
        if (!Guid.TryParse(id, out var jobId))
        {
            return new CronEditResult(JobId: null, Edited: false, Job: null, Error: $"Refused: '{id}' is not a valid GUID.");
        }

        var edit = new CronJobEdit(name, cronExpression, prompt, enabled);
        try
        {
            var updated = await _scheduler.EditAsync(jobId, edit, cancellationToken);
            if (updated is null)
            {
                return new CronEditResult(JobId: jobId, Edited: false, Job: null, Error: $"No cron job {jobId:D} owned by this agent.");
            }
            return new CronEditResult(
                JobId: jobId,
                Edited: true,
                Job: new CronJobSummary(
                    Id: updated.Id,
                    Name: updated.Name,
                    CronExpression: updated.CronExpression,
                    Prompt: updated.Prompt,
                    Enabled: updated.Enabled,
                    LastFiredAt: updated.LastFiredAt,
                    NextFireAt: updated.NextFireAt));
        }
        catch (ArgumentException ex)
        {
            LogEditFailed(workspace.AgentId, jobId, ex.Message, ex);
            return new CronEditResult(JobId: jobId, Edited: false, Job: null, Error: $"Refused: {ex.Message}");
        }
    }

    [McpServerTool(Name = "cron_trigger", OpenWorld = false)]
    [Description("Forces an immediate fire of one of the calling agent's cron jobs (the same stub-fire path the executor takes on a scheduled tick). Returns a JSON object with the parsed jobId and a fired flag. Updates the job's last-fired-at and recomputes the next fire from the wall clock at trigger time.")]
    public async Task<CronTriggerResult> TriggerCron(
        [Description("Cron job id (GUID, format-D).")] string id,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return new CronTriggerResult(JobId: null, Fired: false, Error: "Refused: cron_trigger requires an authenticated agent on the request.");
        }
        if (!Guid.TryParse(id, out var jobId))
        {
            return new CronTriggerResult(JobId: null, Fired: false, Error: $"Refused: '{id}' is not a valid GUID.");
        }

        var fired = await _scheduler.TriggerAsync(jobId, cancellationToken);
        return fired
            ? new CronTriggerResult(JobId: jobId, Fired: true)
            : new CronTriggerResult(JobId: jobId, Fired: false, Error: $"No cron job {jobId:D} owned by this agent.");
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "cron_schedule failed for agent '{AgentId}': {Message}")]
    private partial void LogScheduleFailed(string agentId, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "cron_edit failed for agent '{AgentId}', job '{JobId}': {Message}")]
    private partial void LogEditFailed(string agentId, Guid jobId, string message, Exception ex);
}
