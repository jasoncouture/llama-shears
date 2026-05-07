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

    [McpServerTool(Name = "cron_schedule")]
    [Description("Schedules a recurring future input for the current agent. The expression is a 5-field cron string (minute hour day-of-month month day-of-week) evaluated in UTC. Today the executor is a stub that logs the would-have-been input rather than actually delivering it; the schedule, expression, and prompt are still persisted across restarts so they materialize once the executor graduates from stub to real fire. Returns the new job's id.")]
    public async Task<string> ScheduleCron(
        [Description("Human-readable handle for this job. Used in list output and log messages.")] string name,
        [Description("Cron expression in 5-field form (e.g. '0 9 * * 1-5' for 09:00 UTC weekdays).")] string cronExpression,
        [Description("Prompt text that will be delivered as the agent's input when the job fires.")] string prompt,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return "Refused: cron_schedule requires an authenticated agent on the request.";
        }

        try
        {
            var job = await _scheduler
                .ScheduleAsync(workspace.AgentId, name, cronExpression, prompt, cancellationToken)
                .ConfigureAwait(false);
            return $"Scheduled '{job.Name}' as id {job.Id:D}; next fire {Format(job.NextFireAt)}.";
        }
        catch (ArgumentException ex)
        {
            LogScheduleFailed(_logger, workspace.AgentId, ex.Message, ex);
            return $"Refused: {ex.Message}";
        }
    }

    private static string Format(DateTimeOffset? when) =>
        when is null ? "n/a" : when.Value.ToString("u", System.Globalization.CultureInfo.InvariantCulture);

    [LoggerMessage(Level = LogLevel.Warning, Message = "cron_schedule failed for agent '{AgentId}': {Message}")]
    private static partial void LogScheduleFailed(ILogger logger, string agentId, string message, Exception ex);
}
