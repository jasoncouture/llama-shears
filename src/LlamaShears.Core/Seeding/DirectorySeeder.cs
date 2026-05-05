using LlamaShears.Core.Abstractions.Seeding;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Seeding;

public sealed partial class DirectorySeeder : IDirectorySeeder
{
    private const string KeepFileName = ".keep";

    private readonly ILogger<DirectorySeeder> _logger;

    public DirectorySeeder(ILogger<DirectorySeeder> logger)
    {
        _logger = logger;
    }

    public void SeedIfEmpty(string sourcePath, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        Directory.CreateDirectory(destinationPath);

        var destinationIsEmpty = !Directory.EnumerateFileSystemEntries(destinationPath).Any();
        if (destinationIsEmpty)
        {
            if (!Directory.Exists(sourcePath))
            {
                LogSourceMissing(_logger, sourcePath, destinationPath);
                throw new DirectoryNotFoundException($"Seeder source path does not exist: {sourcePath}");
            }
            CopyDirectory(sourcePath, destinationPath);
            LogSeedComplete(_logger, destinationPath, sourcePath);
        }
        else
        {
            LogDestinationNotEmpty(_logger, destinationPath);
        }

        EnsureKeepMarker(destinationPath);
    }

    private void EnsureKeepMarker(string destinationPath)
    {
        var keepPath = Path.Combine(destinationPath, KeepFileName);
        if (File.Exists(keepPath))
        {
            return;
        }
        File.WriteAllBytes(keepPath, []);
        LogKeepWritten(_logger, keepPath);
    }

    private void CopyDirectory(string source, string destination)
    {
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, dir);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(target);
            LogDirectoryCreated(_logger, target, dir);
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: false);
            LogFileCopied(_logger, target, file);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Destination {Destination} is non-empty; skipping seed.")]
    private static partial void LogDestinationNotEmpty(ILogger logger, string destination);

    [LoggerMessage(Level = LogLevel.Error, Message = "Seeder source {Source} does not exist; cannot populate empty destination {Destination}.")]
    private static partial void LogSourceMissing(ILogger logger, string source, string destination);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Created directory {Target} (from {Source}).")]
    private static partial void LogDirectoryCreated(ILogger logger, string target, string source);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Copied file {Target} (from {Source}).")]
    private static partial void LogFileCopied(ILogger logger, string target, string source);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Wrote {KeepFile}.")]
    private static partial void LogKeepWritten(ILogger logger, string keepFile);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Destination} from {Source}.")]
    private static partial void LogSeedComplete(ILogger logger, string destination, string source);
}
