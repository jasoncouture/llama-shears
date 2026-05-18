using System.ComponentModel;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Cron;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

[McpServerToolType]
public sealed partial class EditCronTool
{
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ICronScheduler _scheduler;
    private readonly ILogger<EditCronTool> _logger;

    public EditCronTool(IAgentWorkspaceLocator workspace, ICronScheduler scheduler, ILogger<EditCronTool> logger)
    {
        _workspace = workspace;
        _scheduler = scheduler;
        _logger = logger;
    }

    [McpServerTool(Name = "cron_edit")]
    [Description("Edits a cron job belonging to the calling agent. Any unspecified field is left unchanged. Mutating the cron expression revalidates and recomputes the next fire time. Returns a JSON object with the parsed jobId, an edited flag, and the updated job summary on success.")]
    public async Task<CronEditResult> EditCron(
        [Description("Cron job id (GUID, format-D).")] string id,
        [Description("New human-readable name. Leave null to keep current.")] string? name = null,
        [Description("New 5-field cron expression. Leave null to keep current.")] string? cronExpression = null,
        [Description("New prompt text. Leave null to keep current.")] string? prompt = null,
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
            var updated = await _scheduler.EditAsync(workspace.AgentId, jobId, edit, cancellationToken);
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "cron_edit failed for agent '{AgentId}', job '{JobId}': {Message}")]
    private partial void LogEditFailed(string agentId, Guid jobId, string message, Exception ex);
}
