using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Seeding;
using LlamaShears.Hosting;

namespace LlamaShears.Api.Host;

public sealed class TemplateSeedingStartupTask : IHostStartupTask
{
    private const string BundledContentSubpath = "content/templates";

    private readonly IShearsPaths _paths;
    private readonly IDirectorySeeder _seeder;

    public TemplateSeedingStartupTask(IShearsPaths paths, IDirectorySeeder seeder)
    {
        _paths = paths;
        _seeder = seeder;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        var destination = _paths.GetPath(PathKind.Templates);
        var source = Path.Combine(AppContext.BaseDirectory, BundledContentSubpath);
        _seeder.SeedIfEmpty(source, destination);
        return ValueTask.CompletedTask;
    }
}
