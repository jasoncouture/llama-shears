using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Hosting;

public sealed partial class HostRestarter : IHostRestarter
{
    private const string ContainerEnvVar = "DOTNET_RUNNING_IN_CONTAINER";

    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<HostRestarter> _logger;
    private int _requested;

    public HostRestarter(IHostApplicationLifetime lifetime, ILogger<HostRestarter> logger)
    {
        _lifetime = lifetime;
        _logger = logger;
    }

    public void RequestRestart()
    {
        if (Interlocked.Exchange(ref _requested, 1) != 0)
        {
            return;
        }

        var inContainer = string.Equals(
            Environment.GetEnvironmentVariable(ContainerEnvVar),
            "true",
            StringComparison.OrdinalIgnoreCase);

        LogRestartRequested(_logger, inContainer);

        // Hooked on ApplicationStopped so the re-exec / non-zero exit
        // happens after every hosted service has finished shutting down
        // (release ports, flush state). Inside the callback we step out
        // of the .NET runtime via Environment.Exit; the spawned child
        // is independent.
        _lifetime.ApplicationStopped.Register(() => Finalize(inContainer));
        _lifetime.StopApplication();
    }

    private void Finalize(bool inContainer)
    {
        if (inContainer)
        {
            // Container supervisor (Docker restart policy, k8s) handles
            // the actual restart; non-zero exit is the only signal we owe.
            LogContainerExit(_logger);
            Environment.Exit(1);
            return;
        }

        var entrypoint = Environment.ProcessPath;
        if (string.IsNullOrEmpty(entrypoint))
        {
            // Should not happen on supported runtimes; fall back to a
            // non-zero exit and rely on whatever launched the host to
            // notice.
            LogNoEntrypoint(_logger);
            Environment.Exit(1);
            return;
        }

        try
        {
            var args = Environment.GetCommandLineArgs();
            var psi = new ProcessStartInfo
            {
                FileName = entrypoint,
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
            };
            // GetCommandLineArgs[0] is the program path itself; argv[1..]
            // is what the user actually passed. Re-pass that verbatim.
            for (var i = 1; i < args.Length; i++)
            {
                psi.ArgumentList.Add(args[i]);
            }

            using var spawned = Process.Start(psi);
            LogReExecuted(_logger, entrypoint, spawned?.Id ?? -1);
        }
        catch (Exception ex)
        {
            LogReExecFailed(_logger, ex);
            Environment.Exit(1);
            return;
        }

        Environment.Exit(0);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Host restart requested (in-container={InContainer}); stopping application.")]
    private static partial void LogRestartRequested(ILogger logger, bool inContainer);

    [LoggerMessage(Level = LogLevel.Information, Message = "Host restart: in container, exiting non-zero so the supervisor restarts us.")]
    private static partial void LogContainerExit(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Host restart: Environment.ProcessPath is empty; cannot re-execute entrypoint, exiting non-zero instead.")]
    private static partial void LogNoEntrypoint(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Host restart: re-executed '{Entrypoint}' as pid {Pid}; exiting current process.")]
    private static partial void LogReExecuted(ILogger logger, string entrypoint, int pid);

    [LoggerMessage(Level = LogLevel.Error, Message = "Host restart: failed to re-execute the entrypoint.")]
    private static partial void LogReExecFailed(ILogger logger, Exception ex);
}
