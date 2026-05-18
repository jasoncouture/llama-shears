using System.Collections.Immutable;
using System.ComponentModel;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Cron;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

[McpServerToolType]
public sealed class ListCronTool
{
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ICronScheduler _scheduler;

    public ListCronTool(IAgentWorkspaceLocator workspace, ICronScheduler scheduler)
    {
        _workspace = workspace;
        _scheduler = scheduler;
    }

    [McpServerTool(Name = "cron_list")]
    [Description("Returns the calling agent's cron jobs as a JSON object: jobCount plus an array of jobs (id, name, cronExpression, prompt, enabled, lastFiredAt, nextFireAt). Other agents' jobs are not visible.")]
    public async Task<CronListResult> ListCron(CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return new CronListResult(JobCount: 0, Jobs: [], Error: "Refused: cron_list requires an authenticated agent on the request.");
        }

        var jobs = await _scheduler.ListByAgentAsync(workspace.AgentId, cancellationToken);
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
}
