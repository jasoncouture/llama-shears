using LlamaShears.Hosting;
using LlamaShears.Hosting.Abstractions;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Api.Host;

public sealed partial class TemplateSeedingStartupTask : IHostStartupTask
{
    private const string KeepFileName = ".keep";
    private const string BundledContentSubpath = "content/templates";

    private readonly ILogger<TemplateSeedingStartupTask> _logger;

    public TemplateSeedingStartupTask(ILogger<TemplateSeedingStartupTask> logger)
    {
        _logger = logger;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        var destination = LlamaShearsPaths.TemplatesRoot;
        var source = Path.Combine(AppContext.BaseDirectory, BundledContentSubpath);

        SeedIfEmpty(source, destination, _logger);

        return ValueTask.CompletedTask;
    }

    public static void SeedIfEmpty(string source, string destination, ILogger logger)
    {
        Directory.CreateDirectory(destination);

        if (Directory.EnumerateFileSystemEntries(destination).Any())
        {
            LogDestinationNotEmpty(logger, destination);
            return;
        }

        if (!Directory.Exists(source))
        {
            LogBundledSourceMissing(logger, source);
            return;
        }

        CopyDirectory(source, destination, logger);
        var keepPath = Path.Combine(destination, KeepFileName);
        File.WriteAllBytes(keepPath, []);
        LogKeepWritten(logger, keepPath);

        LogSeedComplete(logger, destination, source);
    }

    private static void CopyDirectory(string source, string destination, ILogger logger)
    {
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, dir);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(target);
            LogDirectoryCreated(logger, target, dir);
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: false);
            LogFileCopied(logger, target, file);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Templates root {Destination} is non-empty; skipping seed.")]
    private static partial void LogDestinationNotEmpty(ILogger logger, string destination);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Bundled template source {Source} not found; templates root left unseeded.")]
    private static partial void LogBundledSourceMissing(ILogger logger, string source);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Created directory {Target} (from {Source}).")]
    private static partial void LogDirectoryCreated(ILogger logger, string target, string source);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Copied file {Target} (from {Source}).")]
    private static partial void LogFileCopied(ILogger logger, string target, string source);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Wrote {KeepFile}.")]
    private static partial void LogKeepWritten(ILogger logger, string keepFile);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded templates root {Destination} from bundled templates at {Source}.")]
    private static partial void LogSeedComplete(ILogger logger, string destination, string source);
}
