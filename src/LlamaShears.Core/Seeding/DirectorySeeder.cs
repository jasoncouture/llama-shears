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
                LogSourceMissing(sourcePath, destinationPath);
                throw new DirectoryNotFoundException($"Seeder source path does not exist: {sourcePath}");
            }
            CopyDirectory(sourcePath, destinationPath);
            LogSeedComplete(destinationPath, sourcePath);
        }
        else
        {
            LogDestinationNotEmpty(destinationPath);
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
        LogKeepWritten(keepPath);
    }

    private void CopyDirectory(string source, string destination)
    {
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, dir);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(target);
            LogDirectoryCreated(target, dir);
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: false);
            LogFileCopied(target, file);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Destination {Destination} is non-empty; skipping seed.")]
    private partial void LogDestinationNotEmpty(string destination);

    [LoggerMessage(Level = LogLevel.Error, Message = "Seeder source {Source} does not exist; cannot populate empty destination {Destination}.")]
    private partial void LogSourceMissing(string source, string destination);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Created directory {Target} (from {Source}).")]
    private partial void LogDirectoryCreated(string target, string source);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Copied file {Target} (from {Source}).")]
    private partial void LogFileCopied(string target, string source);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Wrote {KeepFile}.")]
    private partial void LogKeepWritten(string keepFile);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Destination} from {Source}.")]
    private partial void LogSeedComplete(string destination, string source);
}
