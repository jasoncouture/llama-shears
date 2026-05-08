using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Paths;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;

public sealed class DeleteFileToolTests
{
    [Test]
    public async Task DeletesExistingFile()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "x");
        var tool = new DeleteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.AllowAll, NullLogger<DeleteFileTool>.Instance);

        var result = await tool.DeleteFile("a.txt", recursive: false, CancellationToken.None);

        await Assert.That(result).Contains("Deleted file");
        await Assert.That(File.Exists(temp.PathOf("a.txt"))).IsFalse();
    }

    [Test]
    public async Task DeletesEmptyDirectoryWithoutRecursive()
    {
        using var temp = TempWorkspace.Create();
        Directory.CreateDirectory(temp.PathOf("empty"));
        var tool = new DeleteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.AllowAll, NullLogger<DeleteFileTool>.Instance);

        var result = await tool.DeleteFile("empty", recursive: false, CancellationToken.None);

        await Assert.That(result).Contains("Deleted directory");
        await Assert.That(Directory.Exists(temp.PathOf("empty"))).IsFalse();
    }

    [Test]
    public async Task DeletesNonEmptyDirectoryWithRecursive()
    {
        using var temp = TempWorkspace.Create();
        Directory.CreateDirectory(temp.PathOf("d"));
        await File.WriteAllTextAsync(temp.PathOf("d", "child.txt"), "x");
        var tool = new DeleteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.AllowAll, NullLogger<DeleteFileTool>.Instance);

        var result = await tool.DeleteFile("d", recursive: true, CancellationToken.None);

        await Assert.That(result).Contains("Deleted directory");
        await Assert.That(Directory.Exists(temp.PathOf("d"))).IsFalse();
    }

    [Test]
    public async Task RefusesDeletingInsideSystemSubfolder()
    {
        using var temp = TempWorkspace.Create();
        Directory.CreateDirectory(temp.PathOf("system"));
        await File.WriteAllTextAsync(temp.PathOf("system", "x.md"), "x");
        var tool = new DeleteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.AllowAll, NullLogger<DeleteFileTool>.Instance);

        var result = await tool.DeleteFile("system/x.md", recursive: false, CancellationToken.None);

        await Assert.That(result).Contains("'system/'");
        await Assert.That(File.Exists(temp.PathOf("system", "x.md"))).IsTrue();
    }

    [Test]
    public async Task RefusesDeletingDotGitDirectoryUnderDefaults()
    {
        using var temp = TempWorkspace.Create();
        Directory.CreateDirectory(temp.PathOf(".git"));
        await File.WriteAllTextAsync(temp.PathOf(".git", "HEAD"), "ref: refs/heads/main");
        var tool = new DeleteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.WorkspaceDefaults, NullLogger<DeleteFileTool>.Instance);

        var result = await tool.DeleteFile(".git", recursive: true, CancellationToken.None);

        await Assert.That(result).Contains("Refused");
        await Assert.That(Directory.Exists(temp.PathOf(".git"))).IsTrue();
    }

    [Test]
    public async Task RefusesDeletingFileInsideDotGitUnderDefaults()
    {
        using var temp = TempWorkspace.Create();
        Directory.CreateDirectory(temp.PathOf(".git"));
        await File.WriteAllTextAsync(temp.PathOf(".git", "HEAD"), "ref: refs/heads/main");
        var tool = new DeleteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.WorkspaceDefaults, NullLogger<DeleteFileTool>.Instance);

        var result = await tool.DeleteFile(".git/HEAD", recursive: false, CancellationToken.None);

        await Assert.That(result).Contains("Refused");
        await Assert.That(File.Exists(temp.PathOf(".git", "HEAD"))).IsTrue();
    }

    [Test]
    public async Task RefusesDeletingNestedDotGitUnderDefaults()
    {
        using var temp = TempWorkspace.Create();
        Directory.CreateDirectory(temp.PathOf("submodule", ".git"));
        await File.WriteAllTextAsync(temp.PathOf("submodule", ".git", "HEAD"), "ref: refs/heads/main");
        var tool = new DeleteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.WorkspaceDefaults, NullLogger<DeleteFileTool>.Instance);

        var result = await tool.DeleteFile("submodule/.git/HEAD", recursive: false, CancellationToken.None);

        await Assert.That(result).Contains("Refused");
        await Assert.That(File.Exists(temp.PathOf("submodule", ".git", "HEAD"))).IsTrue();
    }

    [Test]
    public async Task RefusesDeletingRootMarkdownUnderDefaults()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("README.md"), "x");
        var tool = new DeleteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.WorkspaceDefaults, NullLogger<DeleteFileTool>.Instance);

        var result = await tool.DeleteFile("README.md", recursive: false, CancellationToken.None);

        await Assert.That(result).Contains("Refused");
        await Assert.That(File.Exists(temp.PathOf("README.md"))).IsTrue();
    }

    [Test]
    public async Task AllowsDeletingMarkdownInSubdirectoryUnderDefaults()
    {
        using var temp = TempWorkspace.Create();
        Directory.CreateDirectory(temp.PathOf("docs"));
        await File.WriteAllTextAsync(temp.PathOf("docs", "notes.md"), "x");
        var tool = new DeleteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.WorkspaceDefaults, NullLogger<DeleteFileTool>.Instance);

        var result = await tool.DeleteFile("docs/notes.md", recursive: false, CancellationToken.None);

        await Assert.That(result).Contains("Deleted file");
        await Assert.That(File.Exists(temp.PathOf("docs", "notes.md"))).IsFalse();
    }

    [Test]
    public async Task RefusesDeletingRootGitignoreUnderDefaults()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf(".gitignore"), "bin/\n");
        var tool = new DeleteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.WorkspaceDefaults, NullLogger<DeleteFileTool>.Instance);

        var result = await tool.DeleteFile(".gitignore", recursive: false, CancellationToken.None);

        await Assert.That(result).Contains("Refused");
        await Assert.That(File.Exists(temp.PathOf(".gitignore"))).IsTrue();
    }

    [Test]
    public async Task ReturnsNotFoundWhenPathDoesNotExist()
    {
        using var temp = TempWorkspace.Create();
        var tool = new DeleteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.AllowAll, NullLogger<DeleteFileTool>.Instance);

        var result = await tool.DeleteFile("nope.txt", recursive: false, CancellationToken.None);

        await Assert.That(result).Contains("not found");
    }
}
