using LlamaShears.Api.Host;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Hosting;

public sealed class TemplateSeedingStartupTaskTests
{
    [Test]
    public async Task SeedIfEmpty_copies_directory_tree_and_writes_keep_when_destination_is_empty()
    {
        using var fixture = new SeedingFixture();

        File.WriteAllText(Path.Combine(fixture.Source, "ROOT.md"), "root");
        Directory.CreateDirectory(Path.Combine(fixture.Source, "workspace"));
        File.WriteAllText(Path.Combine(fixture.Source, "workspace", "INNER.md"), "inner");

        TemplateSeedingStartupTask.SeedIfEmpty(fixture.Source, fixture.Destination, NullLogger.Instance);

        await Assert.That(File.Exists(Path.Combine(fixture.Destination, "ROOT.md"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(fixture.Destination, "workspace", "INNER.md"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(fixture.Destination, ".keep"))).IsTrue();
    }

    [Test]
    public async Task SeedIfEmpty_skips_when_destination_contains_only_keep()
    {
        using var fixture = new SeedingFixture();

        File.WriteAllText(Path.Combine(fixture.Source, "ROOT.md"), "root");
        Directory.CreateDirectory(fixture.Destination);
        File.WriteAllBytes(Path.Combine(fixture.Destination, ".keep"), []);

        TemplateSeedingStartupTask.SeedIfEmpty(fixture.Source, fixture.Destination, NullLogger.Instance);

        await Assert.That(File.Exists(Path.Combine(fixture.Destination, "ROOT.md"))).IsFalse();
    }

    [Test]
    public async Task SeedIfEmpty_skips_when_destination_already_has_files()
    {
        using var fixture = new SeedingFixture();

        File.WriteAllText(Path.Combine(fixture.Source, "ROOT.md"), "root");
        Directory.CreateDirectory(fixture.Destination);
        File.WriteAllText(Path.Combine(fixture.Destination, "EXISTING.md"), "existing");

        TemplateSeedingStartupTask.SeedIfEmpty(fixture.Source, fixture.Destination, NullLogger.Instance);

        await Assert.That(File.ReadAllText(Path.Combine(fixture.Destination, "EXISTING.md"))).IsEqualTo("existing");
        await Assert.That(File.Exists(Path.Combine(fixture.Destination, "ROOT.md"))).IsFalse();
        await Assert.That(File.Exists(Path.Combine(fixture.Destination, ".keep"))).IsFalse();
    }

    [Test]
    public async Task SeedIfEmpty_creates_empty_destination_when_source_missing_but_does_not_throw()
    {
        using var fixture = new SeedingFixture(createSource: false);

        TemplateSeedingStartupTask.SeedIfEmpty(fixture.Source, fixture.Destination, NullLogger.Instance);

        await Assert.That(Directory.Exists(fixture.Destination)).IsTrue();
        await Assert.That(File.Exists(Path.Combine(fixture.Destination, ".keep"))).IsFalse();
    }

    private sealed class SeedingFixture : IDisposable
    {
        private readonly string _root;

        public SeedingFixture(bool createSource = true)
        {
            _root = Path.Combine(Path.GetTempPath(), $"llamashears-seed-{Guid.NewGuid():N}");
            Source = Path.Combine(_root, "source");
            Destination = Path.Combine(_root, "destination");
            if (createSource)
            {
                Directory.CreateDirectory(Source);
            }
        }

        public string Source { get; }

        public string Destination { get; }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
