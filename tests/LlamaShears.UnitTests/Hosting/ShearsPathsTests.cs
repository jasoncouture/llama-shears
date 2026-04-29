using LlamaShears.Hosting;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests.Hosting;

public sealed class ShearsPathsTests
{
    [Test]
    public async Task DataRoot_defaults_to_dotted_directory_under_the_user_profile()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        await Assert.That(Path.GetDirectoryName(paths.DataRoot)).IsEqualTo(userProfile);
        await Assert.That(Path.GetFileName(paths.DataRoot)).IsEqualTo(".llama-shears");
    }

    [Test]
    public async Task TemplatesRoot_defaults_under_DataRoot()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        await Assert.That(Path.GetDirectoryName(paths.TemplatesRoot)).IsEqualTo(paths.DataRoot);
        await Assert.That(Path.GetFileName(paths.TemplatesRoot)).IsEqualTo("templates");
    }

    [Test]
    public async Task WorkspaceRoot_defaults_under_DataRoot()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        await Assert.That(Path.GetDirectoryName(paths.WorkspaceRoot)).IsEqualTo(paths.DataRoot);
        await Assert.That(Path.GetFileName(paths.WorkspaceRoot)).IsEqualTo("workspace");
    }

    [Test]
    public async Task AgentsRoot_defaults_under_DataRoot()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        await Assert.That(Path.GetDirectoryName(paths.AgentsRoot)).IsEqualTo(paths.DataRoot);
        await Assert.That(Path.GetFileName(paths.AgentsRoot)).IsEqualTo("agents");
    }

    [Test]
    public async Task Configured_DataRoot_overrides_default_and_descendants_anchor_under_it()
    {
        using var fixture = new TempRoot();
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions { DataRoot = fixture.Path }));

        await Assert.That(paths.DataRoot).IsEqualTo(fixture.Path);
        await Assert.That(Path.GetDirectoryName(paths.WorkspaceRoot)).IsEqualTo(fixture.Path);
        await Assert.That(Path.GetDirectoryName(paths.AgentsRoot)).IsEqualTo(fixture.Path);
        await Assert.That(Path.GetDirectoryName(paths.TemplatesRoot)).IsEqualTo(fixture.Path);
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

        await Assert.That(paths.WorkspaceRoot).IsEqualTo(workspace.Path);
        await Assert.That(Path.GetDirectoryName(paths.AgentsRoot)).IsEqualTo(data.Path);
    }

    [Test]
    public async Task GetAgentWorkspaceDefaultPath_returns_WorkspaceRoot_agentName()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        var path = paths.GetAgentWorkspaceDefaultPath("alpha");

        await Assert.That(path).IsEqualTo(Path.Combine(paths.WorkspaceRoot, "alpha"));
    }

    [Test]
    public async Task GetAgentWorkspaceDefaultPath_does_not_create_the_directory()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));
        var agentName = $"unit-test-{Guid.NewGuid():N}";

        var path = paths.GetAgentWorkspaceDefaultPath(agentName);

        await Assert.That(Directory.Exists(path)).IsFalse();
    }

    [Test]
    public async Task GetAgentWorkspaceDefaultPath_rejects_blank_agent_name()
    {
        var paths = new ShearsPaths(Options.Create(new ShearsPathsOptions()));

        await Assert.That(() => paths.GetAgentWorkspaceDefaultPath(""))
            .Throws<ArgumentException>();
        await Assert.That(() => paths.GetAgentWorkspaceDefaultPath("   "))
            .Throws<ArgumentException>();
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
