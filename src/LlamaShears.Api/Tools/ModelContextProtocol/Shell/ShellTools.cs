using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Shell;

[McpServerToolType]
public sealed partial class ShellTools
{
    private static readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);
    private const int TailLineCount = 50;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ILogger<ShellTools> _logger;

    public ShellTools(IAgentWorkspaceLocator workspace, ILogger<ShellTools> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    [McpServerTool(Name = "shell_sh")]
    [Description("Runs a shell command via /bin/sh -c. Defaults to the agent's workspace as the working directory; pass workingDirectory to override (relative paths resolve against the workspace, absolute paths are honored as-is). Combined stdout+stderr is captured in memory under a lock, preserving shell-like arrival order. Output beyond the shared response budget is truncated to a head+tail snippet at line boundaries. Hard 5-minute timeout; on timeout the entire process tree is killed and whatever has been buffered is returned. stdin is closed immediately so interactive commands fail fast. No allow/deny list, no sandbox.")]
    public Task<string> RunShellAsync(
        [Description("Shell command to execute. Passed verbatim to /bin/sh -c.")] string command,
        [Description("Working directory for the command. Null or empty defaults to the agent's workspace. Relative paths resolve against the workspace; absolute paths are used as-is.")] string? workingDirectory = null,
        CancellationToken cancellationToken = default)
        => RunAsync("/bin/sh", command, workingDirectory, cancellationToken);

    [McpServerTool(Name = "shell_bash")]
    [Description("Runs a bash command via /bin/bash -c. Defaults to the agent's workspace as the working directory; pass workingDirectory to override (relative paths resolve against the workspace, absolute paths are honored as-is). Combined stdout+stderr is captured in memory under a lock, preserving shell-like arrival order. Output beyond the shared response budget is truncated to a head+tail snippet at line boundaries. Hard 5-minute timeout; on timeout the entire process tree is killed and whatever has been buffered is returned. stdin is closed immediately so interactive commands fail fast. No allow/deny list, no sandbox.")]
    public Task<string> RunBashAsync(
        [Description("Bash command to execute. Passed verbatim to /bin/bash -c.")] string command,
        [Description("Working directory for the command. Null or empty defaults to the agent's workspace. Relative paths resolve against the workspace; absolute paths are used as-is.")] string? workingDirectory = null,
        CancellationToken cancellationToken = default)
        => RunAsync("/bin/bash", command, workingDirectory, cancellationToken);

    private async Task<string> RunAsync(string shellPath, string command, string? workingDirectory, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return "Refused: command is required.";
        }

        var workspace = await _workspace.GetAsync(cancellationToken);
        var resolvedCwd = string.IsNullOrWhiteSpace(workingDirectory)
            ? workspace.Root
            : Path.IsPathRooted(workingDirectory)
                ? workingDirectory
                : Path.GetFullPath(Path.Combine(workspace.Root, workingDirectory));
        LogStarting(workspace.AgentId, shellPath, command);

        var startInfo = new ProcessStartInfo
        {
            FileName = shellPath,
            WorkingDirectory = resolvedCwd,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(command);

        var startedAt = Stopwatch.GetTimestamp();

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            return "Failed to start process.";
        }
        process.StandardInput.Close();

        var buffer = new StringBuilder();
        var sync = new object();
        var stdoutPump = PumpAsync(process.StandardOutput, buffer, sync);
        var stderrPump = PumpAsync(process.StandardError, buffer, sync);

        using var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellationTokenSource.CancelAfter(_timeout);
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (timeoutCancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            try { await process.WaitForExitAsync(CancellationToken.None); }
            catch (InvalidOperationException) { }
        }

        await Task.WhenAll(stdoutPump, stderrPump);

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var exitCode = process.HasExited ? process.ExitCode : -1;
        var raw = buffer.ToString();
        var (body, truncated, totalLines) = ApplyBudget(raw);

        LogFinished(workspace.AgentId, shellPath, command, exitCode, timedOut, raw.Length, truncated);
        var header = BuildHeader(exitCode, elapsed, timedOut);
        return $"{header}\n{body}";
    }

    private static (string Body, bool Truncated, int TotalLines) ApplyBudget(string raw)
    {
        if (raw.Length == 0)
        {
            return (string.Empty, Truncated: false, TotalLines: 0);
        }
        var lines = raw.Split('\n');
        var headBytes = 0;
        var headLines = new List<string>(ResponseBudget.MaxLines);
        foreach (var line in lines)
        {
            if (!ResponseBudget.CanAppendResponse(headBytes, headLines.Count, line))
            {
                break;
            }
            headLines.Add(line);
            headBytes += line.Length + 1;
        }

        if (headLines.Count == lines.Length)
        {
            return (string.Join('\n', headLines), Truncated: false, TotalLines: lines.Length);
        }

        var tailStart = Math.Max(headLines.Count, lines.Length - TailLineCount);
        var droppedLines = tailStart - headLines.Count;
        var tail = lines.AsSpan(tailStart).ToArray();

        var stitched = new StringBuilder();
        stitched.Append(string.Join('\n', headLines));
        if (headLines.Count > 0)
        {
            stitched.Append('\n');
        }
        stitched.Append($"[... truncated {droppedLines} line(s); response budget reached ...]");
        if (tail.Length > 0)
        {
            stitched.Append('\n');
            stitched.Append(string.Join('\n', tail));
        }
        return (stitched.ToString(), Truncated: true, TotalLines: lines.Length);
    }

    private static string BuildHeader(int exitCode, TimeSpan elapsed, bool timedOut)
    {
        var ms = (long)elapsed.TotalMilliseconds;
        var status = timedOut ? "timed out" : $"exit code {exitCode}";
        return $"[{status}, execution time {ms}ms]";
    }

    private static async Task PumpAsync(StreamReader source, StringBuilder sink, object sync)
    {
        var buffer = new char[4096];
        while (true)
        {
            int read;
            try
            {
                read = await source.ReadAsync(buffer);
            }
            catch (IOException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            if (read == 0)
            {
                return;
            }
            lock (sync)
            {
                sink.Append(buffer, 0, read);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' running {Shell}: {Command}")]
    private partial void LogStarting(string? agentId, string shell, string command);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' {Shell} finished (exit={ExitCode}, timedOut={TimedOut}, bytes={Bytes}, truncated={Truncated}): {Command}")]
    private partial void LogFinished(string? agentId, string shell, string command, int exitCode, bool timedOut, long bytes, bool truncated);
}
