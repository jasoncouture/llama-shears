using LlamaShears.Hosting;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests.Hosting;

public sealed class ShearsPathsTests
{
    [Test]
    public async Task DataRootDefaultsToDottedDirectoryUnderTheUserProfile()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        var dataRoot = paths.GetPath(PathKind.Data);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        await Assert.That(Path.GetDirectoryName(dataRoot)).IsEqualTo(userProfile);
        await Assert.That(Path.GetFileName(dataRoot)).IsEqualTo(".llama-shears");
    }

    [Test]
    [Arguments(PathKind.Workspace, "workspace")]
    [Arguments(PathKind.Agents, "agents")]
    [Arguments(PathKind.Templates, "templates")]
    [Arguments(PathKind.Context, "context")]
    public async Task SubrootDefaultsUnderDataRoot(PathKind kind, string folderName)
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        var subroot = paths.GetPath(kind);

        await Assert.That(Path.GetDirectoryName(subroot)).IsEqualTo(paths.GetPath(PathKind.Data));
        await Assert.That(Path.GetFileName(subroot)).IsEqualTo(folderName);
    }

    [Test]
    public async Task ConfiguredDataRootOverridesDefaultAndDescendantsAnchorUnderIt()
    {
        using var fixture = new TempRoot();
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions { DataRoot = fixture.Path }));

        await Assert.That(paths.GetPath(PathKind.Data)).IsEqualTo(fixture.Path);
        await Assert.That(Path.GetDirectoryName(paths.GetPath(PathKind.Workspace))).IsEqualTo(fixture.Path);
        await Assert.That(Path.GetDirectoryName(paths.GetPath(PathKind.Agents))).IsEqualTo(fixture.Path);
        await Assert.That(Path.GetDirectoryName(paths.GetPath(PathKind.Templates))).IsEqualTo(fixture.Path);
        await Assert.That(Path.GetDirectoryName(paths.GetPath(PathKind.Context))).IsEqualTo(fixture.Path);
    }

    [Test]
    public async Task ConfiguredSubrootOverridesTheDefaultSubfolderOnly()
    {
        using var data = new TempRoot();
        using var workspace = new TempRoot();

        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions
        {
            DataRoot = data.Path,
            WorkspaceRoot = workspace.Path,
        }));

        await Assert.That(paths.GetPath(PathKind.Workspace)).IsEqualTo(workspace.Path);
        await Assert.That(Path.GetDirectoryName(paths.GetPath(PathKind.Agents))).IsEqualTo(data.Path);
    }

    [Test]
    public async Task GetPathCombinesRootWithSubpath()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        var combined = paths.GetPath(PathKind.Templates, "spec/v1");

        await Assert.That(combined).IsEqualTo(Path.Combine(paths.GetPath(PathKind.Templates), "spec/v1"));
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task GetPathTreatsBlankSubpathAsRoot(string? subpath)
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        await Assert.That(paths.GetPath(PathKind.Agents, subpath)).IsEqualTo(paths.GetPath(PathKind.Agents));
    }

    [Test]
    public async Task GetPathDoesNotCreateTheSubpathDirectory()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));
        var subpath = $"unit-test-{Guid.NewGuid():N}";

        var path = paths.GetPath(PathKind.Workspace, subpath);

        await Assert.That(Directory.Exists(path)).IsFalse();
    }

    [Test]
    public async Task GetPathWithEnsureExistsCreatesTheSubpathDirectory()
    {
        using var fixture = new TempRoot();
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions { DataRoot = fixture.Path }));
        var subpath = $"unit-test-{Guid.NewGuid():N}";

        var path = paths.GetPath(PathKind.Workspace, subpath, ensureExists: true);

        try
        {
            await Assert.That(Directory.Exists(path)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }

    [Test]
    public async Task GetPathThrowsForUnknownKind()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        await Assert.That(() => paths.GetPath((PathKind)999))
            .Throws<ArgumentOutOfRangeException>();
    }

    private sealed class TempRoot : IDisposable
    {
        public TempRoot()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"llamashears-paths-{Guid.NewGuid():N}");
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
