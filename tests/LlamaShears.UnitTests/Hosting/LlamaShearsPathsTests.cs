using LlamaShears.Hosting.Abstractions;

namespace LlamaShears.UnitTests.Hosting;

public sealed class LlamaShearsPathsTests
{
    [Test]
    public async Task DataRoot_is_dotted_directory_under_the_user_profile()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var dataRoot = LlamaShearsPaths.DataRoot;

        await Assert.That(Path.GetDirectoryName(dataRoot)).IsEqualTo(userProfile);
        await Assert.That(Path.GetFileName(dataRoot)).IsEqualTo(".llama-shears");
    }

    [Test]
    public async Task ConfigFile_lives_directly_under_DataRoot()
    {
        var configFile = LlamaShearsPaths.ConfigFile;

        await Assert.That(Path.GetDirectoryName(configFile)).IsEqualTo(LlamaShearsPaths.DataRoot);
        await Assert.That(Path.GetFileName(configFile)).IsEqualTo("config.json");
    }

    [Test]
    public async Task TemplatesRoot_lives_directly_under_DataRoot()
    {
        var templatesRoot = LlamaShearsPaths.TemplatesRoot;

        await Assert.That(Path.GetDirectoryName(templatesRoot)).IsEqualTo(LlamaShearsPaths.DataRoot);
        await Assert.That(Path.GetFileName(templatesRoot)).IsEqualTo("templates");
    }

    [Test]
    public async Task GetAgentWorkspaceDefaultPath_returns_DataRoot_workspaces_agentName()
    {
        var path = LlamaShearsPaths.GetAgentWorkspaceDefaultPath("alpha");

        var expected = Path.Combine(LlamaShearsPaths.DataRoot, "workspaces", "alpha");
        await Assert.That(path).IsEqualTo(expected);
    }

    [Test]
    public async Task GetAgentWorkspaceDefaultPath_does_not_create_the_directory()
    {
        var agentName = $"unit-test-{Guid.NewGuid():N}";

        var path = LlamaShearsPaths.GetAgentWorkspaceDefaultPath(agentName);

        // The agent-specific directory must not be auto-created. The parent
        // `workspaces/` may exist from earlier runs; the contract is only
        // that *this call* doesn't materialize anything.
        await Assert.That(Directory.Exists(path)).IsFalse();
    }

    [Test]
    public async Task GetAgentWorkspaceDefaultPath_rejects_blank_agent_name()
    {
        await Assert.That(() => LlamaShearsPaths.GetAgentWorkspaceDefaultPath(""))
            .Throws<ArgumentException>();
        await Assert.That(() => LlamaShearsPaths.GetAgentWorkspaceDefaultPath("   "))
            .Throws<ArgumentException>();
    }
}
