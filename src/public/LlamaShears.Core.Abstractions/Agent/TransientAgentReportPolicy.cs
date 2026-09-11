namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Controls whether a transient agent forwards its last assistant
/// text to the parent session when it goes idle. Absent from the
/// data scope means report-on (cron and heartbeat). Await-style
/// <c>subagent_run</c> sets <see cref="ReportToParent"/> to
/// <see langword="false"/> so the caller collects the output once.
/// </summary>
/// <param name="ReportToParent">When <see langword="false"/>, skip the parent <c>ChannelMessage</c>.</param>
public sealed record TransientAgentReportPolicy(bool ReportToParent)
{
    /// <summary>Data-scope key for the report policy.</summary>
    public const string DataKey = "transient_agent_report_policy";
}
