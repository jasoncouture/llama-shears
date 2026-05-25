using System.ComponentModel;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Cron;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

[McpServerToolType]
public sealed partial class ScheduleCronTool
{
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ICronScheduler _scheduler;
    private readonly ILogger<ScheduleCronTool> _logger;

    public ScheduleCronTool(IAgentWorkspaceLocator workspace, ICronScheduler scheduler, ILogger<ScheduleCronTool> logger)
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "cron_schedule failed for agent '{AgentId}': {Message}")]
    private partial void LogScheduleFailed(string agentId, string message, Exception ex);
}
