using System.ComponentModel;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Cron;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

[McpServerToolType]
public sealed class TriggerCronTool
{
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ICronScheduler _scheduler;

    public TriggerCronTool(IAgentWorkspaceLocator workspace, ICronScheduler scheduler)
    {
        _workspace = workspace;
        _scheduler = scheduler;
    }

    [McpServerTool(Name = "cron_trigger")]
    [Description("Forces an immediate fire of one of the calling agent's cron jobs (the same stub-fire path the executor takes on a scheduled tick). Updates the job's last-fired-at and recomputes the next fire from the wall clock at trigger time.")]
    public async Task<string> TriggerCron(
        [Description("Cron job id (GUID, format-D).")] string id,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return "Refused: cron_trigger requires an authenticated agent on the request.";
        }
        if (!Guid.TryParse(id, out var jobId))
        {
            return $"Refused: '{id}' is not a valid GUID.";
        }

        var fired = await _scheduler.TriggerAsync(workspace.AgentId, jobId, cancellationToken).ConfigureAwait(false);
        return fired
            ? $"Fired cron job {jobId:D} (stub: logged only)."
            : $"No cron job {jobId:D} owned by this agent.";
    }
}
