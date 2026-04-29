using LlamaShears.Hosting;
using LlamaShears.Hosting.Abstractions;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Api.Host;

public sealed class TemplateSeedingStartupTask : IHostStartupTask
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
            logger.LogDebug(
                "Templates root {Destination} is non-empty; skipping seed.",
                destination);
            return;
        }

        if (!Directory.Exists(source))
        {
            logger.LogWarning(
                "Bundled template source {Source} not found; templates root left unseeded.",
                source);
            return;
        }

        CopyDirectory(source, destination);
        File.WriteAllBytes(Path.Combine(destination, KeepFileName), []);

        logger.LogInformation(
            "Seeded templates root {Destination} from bundled templates at {Source}.",
            destination,
            source);
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, dir);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: false);
        }
    }
}
