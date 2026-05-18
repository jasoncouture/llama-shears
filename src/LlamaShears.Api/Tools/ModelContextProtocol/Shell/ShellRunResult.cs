using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Shell;

public sealed record ShellRunResult(
    int ExitCode,
    bool TimedOut,
    double ElapsedMilliseconds,
    int TotalLines,
    bool Truncated,
    string Output,
    string? Error = null) : IToolResponse;
