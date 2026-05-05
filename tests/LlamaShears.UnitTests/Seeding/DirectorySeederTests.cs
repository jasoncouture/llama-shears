using LlamaShears.Core.Seeding;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Seeding;

public sealed class DirectorySeederTests
{
    [Test]
    public async Task CopiesDirectoryTreeAndWritesKeepWhenDestinationIsEmpty()
    {
        using var fixture = new SeedingFixture();
        var seeder = new DirectorySeeder(NullLogger<DirectorySeeder>.Instance);

        File.WriteAllText(Path.Combine(fixture.Source, "ROOT.md"), "root");
        Directory.CreateDirectory(Path.Combine(fixture.Source, "workspace"));
        File.WriteAllText(Path.Combine(fixture.Source, "workspace", "INNER.md"), "inner");

        seeder.SeedIfEmpty(fixture.Source, fixture.Destination);

        await Assert.That(File.Exists(Path.Combine(fixture.Destination, "ROOT.md"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(fixture.Destination, "workspace", "INNER.md"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(fixture.Destination, ".keep"))).IsTrue();
    }

    [Test]
    public async Task SkipsCopyWhenDestinationContainsOnlyKeep()
    {
        using var fixture = new SeedingFixture();
        var seeder = new DirectorySeeder(NullLogger<DirectorySeeder>.Instance);

        File.WriteAllText(Path.Combine(fixture.Source, "ROOT.md"), "root");
        Directory.CreateDirectory(fixture.Destination);
        File.WriteAllBytes(Path.Combine(fixture.Destination, ".keep"), []);

        seeder.SeedIfEmpty(fixture.Source, fixture.Destination);

        await Assert.That(File.Exists(Path.Combine(fixture.Destination, "ROOT.md"))).IsFalse();
        await Assert.That(File.Exists(Path.Combine(fixture.Destination, ".keep"))).IsTrue();
    }

    [Test]
    public async Task SkipsCopyWhenDestinationAlreadyHasFiles()
    {
        using var fixture = new SeedingFixture();
        var seeder = new DirectorySeeder(NullLogger<DirectorySeeder>.Instance);

        File.WriteAllText(Path.Combine(fixture.Source, "ROOT.md"), "root");
        Directory.CreateDirectory(fixture.Destination);
        File.WriteAllText(Path.Combine(fixture.Destination, "EXISTING.md"), "existing");

        seeder.SeedIfEmpty(fixture.Source, fixture.Destination);

        await Assert.That(File.ReadAllText(Path.Combine(fixture.Destination, "EXISTING.md"))).IsEqualTo("existing");
        await Assert.That(File.Exists(Path.Combine(fixture.Destination, "ROOT.md"))).IsFalse();
    }

    [Test]
    public async Task EnsuresKeepInNonEmptyDestinationLackingIt()
    {
        using var fixture = new SeedingFixture();
        var seeder = new DirectorySeeder(NullLogger<DirectorySeeder>.Instance);

        File.WriteAllText(Path.Combine(fixture.Source, "ROOT.md"), "root");
        Directory.CreateDirectory(fixture.Destination);
        File.WriteAllText(Path.Combine(fixture.Destination, "EXISTING.md"), "existing");

        seeder.SeedIfEmpty(fixture.Source, fixture.Destination);

        await Assert.That(File.Exists(Path.Combine(fixture.Destination, ".keep"))).IsTrue();
    }

    [Test]
    public async Task ThrowsWhenSourceMissingAndDestinationIsEmpty()
    {
        using var fixture = new SeedingFixture(createSource: false);
        var seeder = new DirectorySeeder(NullLogger<DirectorySeeder>.Instance);

        await Assert.That(() => seeder.SeedIfEmpty(fixture.Source, fixture.Destination))
            .Throws<DirectoryNotFoundException>();
    }

    [Test]
    public async Task DoesNotThrowWhenSourceMissingButDestinationAlreadyHasContent()
    {
        using var fixture = new SeedingFixture(createSource: false);
        var seeder = new DirectorySeeder(NullLogger<DirectorySeeder>.Instance);

        Directory.CreateDirectory(fixture.Destination);
        File.WriteAllText(Path.Combine(fixture.Destination, "EXISTING.md"), "existing");

        seeder.SeedIfEmpty(fixture.Source, fixture.Destination);

        await Assert.That(File.Exists(Path.Combine(fixture.Destination, ".keep"))).IsTrue();
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
