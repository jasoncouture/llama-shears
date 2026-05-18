using System.Collections.Immutable;
using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

public sealed record CronListResult(
    int JobCount,
    ImmutableArray<CronJobSummary> Jobs,
    string? Error = null) : IToolResponse;
