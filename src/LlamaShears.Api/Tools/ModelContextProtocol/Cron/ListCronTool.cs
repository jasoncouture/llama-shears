using System.ComponentModel;
using System.Globalization;
using System.Text;
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
    [Description("Returns the calling agent's cron jobs — id, name, expression, enabled flag, last/next fire timestamps, and prompt prefix. Other agents' jobs are not visible.")]
    public async Task<string> ListCron(CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return "Refused: cron_list requires an authenticated agent on the request.";
        }

        var jobs = await _scheduler.ListByAgentAsync(workspace.AgentId, cancellationToken).ConfigureAwait(false);
        if (jobs.Count == 0)
        {
            return "No scheduled cron jobs.";
        }

        var builder = new StringBuilder();
        builder.AppendFormat(CultureInfo.InvariantCulture, "{0} cron job(s):", jobs.Count);
        foreach (var job in jobs)
        {
            builder.Append('\n');
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:D}  enabled={1}  expr='{2}'  next={3}  last={4}  name='{5}'  prompt='{6}'",
                job.Id,
                job.Enabled,
                job.CronExpression,
                Format(job.NextFireAt),
                Format(job.LastFiredAt),
                job.Name,
                Truncate(job.Prompt, 80));
        }
        return builder.ToString();
    }

    private static string Format(DateTimeOffset? when) =>
        when is null ? "n/a" : when.Value.ToString("u", CultureInfo.InvariantCulture);

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : string.Concat(value.AsSpan(0, max - 1), "…");
}
