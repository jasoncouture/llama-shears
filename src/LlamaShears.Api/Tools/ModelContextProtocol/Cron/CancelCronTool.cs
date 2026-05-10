using System.ComponentModel;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Cron;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

[McpServerToolType]
public sealed class CancelCronTool
{
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ICronScheduler _scheduler;

    public CancelCronTool(IAgentWorkspaceLocator workspace, ICronScheduler scheduler)
    {
        _workspace = workspace;
        _scheduler = scheduler;
    }

    [McpServerTool(Name = "cron_cancel")]
    [Description("Cancels a cron job belonging to the calling agent. Refuses jobs owned by other agents or unknown ids.")]
    public async Task<string> CancelCron(
        [Description("Cron job id (GUID, format-D).")] string id,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return "Refused: cron_cancel requires an authenticated agent on the request.";
        }
        if (!Guid.TryParse(id, out var jobId))
        {
            return $"Refused: '{id}' is not a valid GUID.";
        }

        var removed = await _scheduler.CancelAsync(workspace.AgentId, jobId, cancellationToken);
        return removed
            ? $"Cancelled cron job {jobId:D}."
            : $"No cron job {jobId:D} owned by this agent.";
    }
}
