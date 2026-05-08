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

        _lifetime.ApplicationStopped.Register(() => FinalizeRestart(inContainer));
        _lifetime.StopApplication();
    }

    private void FinalizeRestart(bool inContainer)
    {
        if (inContainer)
        {
            LogContainerExit(_logger);
            Environment.Exit(1);
            return;
        }

        var entrypoint = Environment.ProcessPath;
        if (string.IsNullOrEmpty(entrypoint))
        {
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
            for (var i = 1; i < args.Length; i++)
            {
                psi.ArgumentList.Add(args[i]);
            }

            using var spawned = Process.Start(psi);
            if (spawned is null)
            {
                LogReExecReturnedNull(_logger, entrypoint);
                Environment.Exit(1);
                return;
            }
            LogReExecuted(_logger, entrypoint, spawned.Id);
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Host restart: Process.Start returned null for '{Entrypoint}'; exiting non-zero so the host doesn't disappear silently.")]
    private static partial void LogReExecReturnedNull(ILogger logger, string entrypoint);

    [LoggerMessage(Level = LogLevel.Error, Message = "Host restart: failed to re-execute the entrypoint.")]
    private static partial void LogReExecFailed(ILogger logger, Exception ex);
}
