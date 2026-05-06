using LlamaShears.Hosting;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests.Hosting;

public sealed class ShearsPathsTests
{
    [Test]
    public async Task DataRoot_defaults_to_dotted_directory_under_the_user_profile()
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
    public async Task Subroot_defaults_under_DataRoot(PathKind kind, string folderName)
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        var subroot = paths.GetPath(kind);

        await Assert.That(Path.GetDirectoryName(subroot)).IsEqualTo(paths.GetPath(PathKind.Data));
        await Assert.That(Path.GetFileName(subroot)).IsEqualTo(folderName);
    }

    [Test]
    public async Task Configured_DataRoot_overrides_default_and_descendants_anchor_under_it()
    {
        using var fixture = new TempRoot();
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions { DataRoot = fixture.Path }));

        await Assert.That(paths.GetPath(PathKind.Data)).IsEqualTo(fixture.Path);
        await Assert.That(Path.GetDirectoryName(paths.GetPath(PathKind.Workspace))).IsEqualTo(fixture.Path);
        await Assert.That(Path.GetDirectoryName(paths.GetPath(PathKind.Agents))).IsEqualTo(fixture.Path);
        await Assert.That(Path.GetDirectoryName(paths.GetPath(PathKind.Templates))).IsEqualTo(fixture.Path);
    }

    [Test]
    public async Task Configured_subroot_overrides_the_default_subfolder_only()
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
    public async Task GetPath_combines_root_with_subpath()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        var combined = paths.GetPath(PathKind.Templates, "spec/v1");

        await Assert.That(combined).IsEqualTo(Path.Combine(paths.GetPath(PathKind.Templates), "spec/v1"));
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task GetPath_treats_blank_subpath_as_root(string? subpath)
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        await Assert.That(paths.GetPath(PathKind.Agents, subpath)).IsEqualTo(paths.GetPath(PathKind.Agents));
    }

    [Test]
    public async Task GetPath_does_not_create_the_subpath_directory()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));
        var subpath = $"unit-test-{Guid.NewGuid():N}";

        var path = paths.GetPath(PathKind.Workspace, subpath);

        await Assert.That(Directory.Exists(path)).IsFalse();
    }

    [Test]
    public async Task GetPath_throws_for_unknown_kind()
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
