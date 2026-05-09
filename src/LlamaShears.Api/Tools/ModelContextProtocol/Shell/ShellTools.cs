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

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ILogger<ShellTools> _logger;

    public ShellTools(IAgentWorkspaceLocator workspace, ILogger<ShellTools> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    [McpServerTool(Name = "shell_sh")]
    [Description("Runs a shell command in the agent's workspace via /bin/sh -c. Combined stdout+stderr is captured at the byte level under a lock, preserving shell-like arrival order. Hard 5-minute timeout; on timeout the entire process tree is killed and whatever has been buffered is returned. stdin is closed immediately so interactive commands fail fast. No allow/deny list, no sandbox.")]
    public Task<string> RunShellAsync(
        [Description("Shell command to execute. Passed verbatim to /bin/sh -c.")] string command,
        CancellationToken cancellationToken = default)
        => RunAsync("/bin/sh", command, cancellationToken);

    [McpServerTool(Name = "shell_bash")]
    [Description("Runs a bash command in the agent's workspace via /bin/bash -c. Combined stdout+stderr is captured at the byte level under a lock, preserving shell-like arrival order. Hard 5-minute timeout; on timeout the entire process tree is killed and whatever has been buffered is returned. stdin is closed immediately so interactive commands fail fast. No allow/deny list, no sandbox.")]
    public Task<string> RunBashAsync(
        [Description("Bash command to execute. Passed verbatim to /bin/bash -c.")] string command,
        CancellationToken cancellationToken = default)
        => RunAsync("/bin/bash", command, cancellationToken);

    private async Task<string> RunAsync(string shellPath, string command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return "Refused: command is required.";
        }

        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        LogStarting(_logger, workspace.AgentId, shellPath, command);

        var startInfo = new ProcessStartInfo
        {
            FileName = shellPath,
            WorkingDirectory = workspace.Root,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(command);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            return "Failed to start process.";
        }
        process.StandardInput.Close();

        var sink = new MemoryStream();
        var sync = new object();
        var stdoutPump = PumpAsync(process.StandardOutput.BaseStream, sink, sync);
        var stderrPump = PumpAsync(process.StandardError.BaseStream, sink, sync);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            try { await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false); }
            catch (InvalidOperationException) { }
        }

        await Task.WhenAll(stdoutPump, stderrPump).ConfigureAwait(false);

        var exitCode = process.HasExited ? process.ExitCode : -1;
        var captured = Encoding.UTF8.GetString(sink.GetBuffer(), 0, (int)sink.Length);
        LogFinished(_logger, workspace.AgentId, shellPath, command, exitCode, timedOut, captured.Length);

        if (timedOut)
        {
            return $"[timed out after {_timeout.TotalMinutes:0} minutes; process tree killed]\n{captured}";
        }
        return $"[exit {exitCode}]\n{captured}";
    }

    private static async Task PumpAsync(Stream source, MemoryStream sink, object sync)
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' {Shell} finished (exit={ExitCode}, timedOut={TimedOut}, bytes={Bytes}): {Command}")]
    private static partial void LogFinished(ILogger logger, string? agentId, string shell, string command, int exitCode, bool timedOut, int bytes);
}
