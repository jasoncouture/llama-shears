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
    [Description("Edits a cron job belonging to the calling agent. Any unspecified field is left unchanged. Mutating the cron expression revalidates and recomputes the next fire time. Returns the updated job summary.")]
    public async Task<string> EditCron(
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
            return "Refused: cron_edit requires an authenticated agent on the request.";
        }
        if (!Guid.TryParse(id, out var jobId))
        {
            return $"Refused: '{id}' is not a valid GUID.";
        }

        var edit = new CronJobEdit(name, cronExpression, prompt, enabled);
        try
        {
            var updated = await _scheduler.EditAsync(workspace.AgentId, jobId, edit, cancellationToken);
            return updated is null
                ? $"No cron job {jobId:D} owned by this agent."
                : $"Edited cron job {updated.Id:D}; enabled={updated.Enabled}; expr='{updated.CronExpression}'; next {Format(updated.NextFireAt)}.";
        }
        catch (ArgumentException ex)
        {
            LogEditFailed(_logger, workspace.AgentId, jobId, ex.Message, ex);
            return $"Refused: {ex.Message}";
        }
    }

    private static string Format(DateTimeOffset? when) =>
        when is null ? "n/a" : when.Value.ToString("u");

    [LoggerMessage(Level = LogLevel.Warning, Message = "cron_edit failed for agent '{AgentId}', job '{JobId}': {Message}")]
    private static partial void LogEditFailed(ILogger logger, string agentId, Guid jobId, string message, Exception ex);
}
