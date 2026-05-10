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
    private static readonly TimeSpan _tempFileTtl = TimeSpan.FromMinutes(30);
    private const string TempFilePrefix = "llamashears-shell-";
    private const string TempFileSuffix = ".log";
    private const string TempFileGlob = "llamashears-shell-*.log";
    private const int OutputCap = 64 * 1024;
    private const int HeadCap = OutputCap / 2;
    private const int TailCap = OutputCap / 2;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ILogger<ShellTools> _logger;

    public ShellTools(IAgentWorkspaceLocator workspace, ILogger<ShellTools> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    [McpServerTool(Name = "shell_sh")]
    [Description("Runs a shell command via /bin/sh -c. Defaults to the agent's workspace as the working directory; pass workingDirectory to override (relative paths resolve against the workspace, absolute paths are honored as-is). Combined stdout+stderr is captured at the byte level under a lock and streamed to a temp file, preserving shell-like arrival order. Output exceeding 64 KiB is truncated to a head+tail snippet at line boundaries with a '[Truncated]' marker, and the full log is preserved at the path reported in 'fullOutput'. Hard 5-minute timeout; on timeout the entire process tree is killed and whatever has been buffered is returned. stdin is closed immediately so interactive commands fail fast. No allow/deny list, no sandbox.")]
    public Task<string> RunShellAsync(
        [Description("Shell command to execute. Passed verbatim to /bin/sh -c.")] string command,
        [Description("Working directory for the command. Null or empty defaults to the agent's workspace. Relative paths resolve against the workspace; absolute paths are used as-is.")] string? workingDirectory = null,
        CancellationToken cancellationToken = default)
        => RunAsync("/bin/sh", command, workingDirectory, cancellationToken);

    [McpServerTool(Name = "shell_bash")]
    [Description("Runs a bash command via /bin/bash -c. Defaults to the agent's workspace as the working directory; pass workingDirectory to override (relative paths resolve against the workspace, absolute paths are honored as-is). Combined stdout+stderr is captured at the byte level under a lock and streamed to a temp file, preserving shell-like arrival order. Output exceeding 64 KiB is truncated to a head+tail snippet at line boundaries with a '[Truncated]' marker, and the full log is preserved at the path reported in 'fullOutput'. Hard 5-minute timeout; on timeout the entire process tree is killed and whatever has been buffered is returned. stdin is closed immediately so interactive commands fail fast. No allow/deny list, no sandbox.")]
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

        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        var resolvedCwd = string.IsNullOrWhiteSpace(workingDirectory)
            ? workspace.Root
            : Path.IsPathRooted(workingDirectory)
                ? workingDirectory
                : Path.GetFullPath(Path.Combine(workspace.Root, workingDirectory));
        LogStarting(_logger, workspace.AgentId, shellPath, command);

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

        SweepStaleTempFiles();
        var tempPath = Path.Combine(Path.GetTempPath(), $"{TempFilePrefix}{Guid.NewGuid():N}{TempFileSuffix}");
        var startedAt = Stopwatch.GetTimestamp();

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            return "Failed to start process.";
        }
        process.StandardInput.Close();

        var sink = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
        try
        {
            var sync = new object();
            var stdoutPump = PumpAsync(process.StandardOutput.BaseStream, sink, sync);
            var stderrPump = PumpAsync(process.StandardError.BaseStream, sink, sync);

            using var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellationTokenSource.CancelAfter(_timeout);
            var timedOut = false;
            try
            {
                await process.WaitForExitAsync(timeoutCancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                timedOut = true;
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                try { await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false); }
                catch (InvalidOperationException) { }
            }

            await Task.WhenAll(stdoutPump, stderrPump).ConfigureAwait(false);
            await sink.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            var fileLength = sink.Length;
            await sink.DisposeAsync().ConfigureAwait(false);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var exitCode = process.HasExited ? process.ExitCode : -1;
            var truncated = fileLength > OutputCap;

            string body;
            if (truncated)
            {
                body = await ReadHeadAndTailAsync(tempPath, fileLength).ConfigureAwait(false);
            }
            else
            {
                body = await File.ReadAllTextAsync(tempPath, Encoding.UTF8, CancellationToken.None).ConfigureAwait(false);
                try { File.Delete(tempPath); }
                catch (IOException) { }
            }

            LogFinished(_logger, workspace.AgentId, shellPath, command, exitCode, timedOut, fileLength, truncated);
            var header = BuildHeader(exitCode, elapsed, timedOut, truncated, tempPath);
            return $"{header}\n{body}";
        }
        catch
        {
            await sink.DisposeAsync().ConfigureAwait(false);
            try { File.Delete(tempPath); }
            catch (IOException) { }
            throw;
        }
    }

    private static string BuildHeader(int exitCode, TimeSpan elapsed, bool timedOut, bool truncated, string tempPath)
    {
        var ms = (long)elapsed.TotalMilliseconds;
        var status = timedOut ? "timed out" : $"exit code {exitCode}";
        if (truncated)
        {
            var ttlMinutes = (long)_tempFileTtl.TotalMinutes;
            return $"[{status}, execution time {ms}ms, fullOutput: {tempPath} (available {ttlMinutes} minutes)]";
        }
        return $"[{status}, execution time {ms}ms]";
    }

    private static void SweepStaleTempFiles()
    {
        var threshold = DateTime.UtcNow - _tempFileTtl;
        try
        {
            foreach (var file in Directory.EnumerateFiles(Path.GetTempPath(), TempFileGlob))
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.LastWriteTimeUtc < threshold)
                    {
                        info.Delete();
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static async Task<string> ReadHeadAndTailAsync(string path, long totalLength)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

        var headBuffer = new byte[HeadCap];
        var headRead = await stream.ReadAtLeastAsync(headBuffer, HeadCap, throwOnEndOfStream: false).ConfigureAwait(false);
        var headEnd = SnapToLastNewline(headBuffer.AsSpan(0, headRead));
        var head = Encoding.UTF8.GetString(headBuffer, 0, headEnd);

        var tailStart = totalLength - TailCap;
        stream.Position = tailStart;
        var tailBuffer = new byte[TailCap];
        var tailRead = await stream.ReadAtLeastAsync(tailBuffer, TailCap, throwOnEndOfStream: false).ConfigureAwait(false);
        var tailOffset = SnapToFirstNewline(tailBuffer.AsSpan(0, tailRead));
        var tail = Encoding.UTF8.GetString(tailBuffer, tailOffset, tailRead - tailOffset);

        var droppedBytes = totalLength - headEnd - (tailRead - tailOffset);
        return $"{head}[Truncated {droppedBytes} bytes; see fullOutput]\n{tail}";
    }

    private static int SnapToLastNewline(ReadOnlySpan<byte> span)
    {
        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (span[i] == (byte)'\n')
            {
                return i + 1;
            }
        }
        return span.Length;
    }

    private static int SnapToFirstNewline(ReadOnlySpan<byte> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == (byte)'\n')
            {
                return i + 1;
            }
        }
        return 0;
    }

    private static async Task PumpAsync(Stream source, FileStream sink, object sync)
    {
        var buffer = new byte[4096];
        while (true)
        {
            int read;
            try
            {
                read = await source.ReadAsync(buffer).ConfigureAwait(false);
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
                sink.Write(buffer, 0, read);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' running {Shell}: {Command}")]
    private static partial void LogStarting(ILogger logger, string? agentId, string shell, string command);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' {Shell} finished (exit={ExitCode}, timedOut={TimedOut}, bytes={Bytes}, truncated={Truncated}): {Command}")]
    private static partial void LogFinished(ILogger logger, string? agentId, string shell, string command, int exitCode, bool timedOut, long bytes, bool truncated);
}
