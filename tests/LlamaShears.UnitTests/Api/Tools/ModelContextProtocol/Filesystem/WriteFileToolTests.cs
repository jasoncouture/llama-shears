using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;

public sealed class WriteFileToolTests
{
    [Test]
    public async Task WritesNewFileAndCreatesParentDirectories()
    {
        using var temp = TempWorkspace.Create();
        var tool = new WriteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<WriteFileTool>.Instance);

        var result = await tool.WriteFile("notes/scratch.txt", "hello", overwrite: false, CancellationToken.None);

        await Assert.That(result).Contains("Wrote");
        await Assert.That(File.Exists(temp.PathOf("notes", "scratch.txt"))).IsTrue();
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("notes", "scratch.txt"))).IsEqualTo("hello");
    }

    [Test]
    public async Task RefusesWhenFileExistsAndOverwriteIsFalse()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "original");
        var tool = new WriteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<WriteFileTool>.Instance);

        var result = await tool.WriteFile("a.txt", "new", overwrite: false, CancellationToken.None);

        await Assert.That(result).Contains("already exists");
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("a.txt"))).IsEqualTo("original");
    }

    [Test]
    public async Task ReplacesExistingFileWhenOverwriteIsTrue()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "original");
        var tool = new WriteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<WriteFileTool>.Instance);

        var result = await tool.WriteFile("a.txt", "new", overwrite: true, CancellationToken.None);

        await Assert.That(result).Contains("Wrote");
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("a.txt"))).IsEqualTo("new");
    }

    [Test]
    public async Task RefusesWritesIntoSystemSubfolder()
    {
        using var temp = TempWorkspace.Create();
        var tool = new WriteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<WriteFileTool>.Instance);

        var result = await tool.WriteFile("system/anything.md", "x", overwrite: true, CancellationToken.None);

        await Assert.That(result).Contains("'system/'");
        await Assert.That(File.Exists(temp.PathOf("system", "anything.md"))).IsFalse();
    }

    [Test]
    public async Task RefusesWritesOutsideWorkspace()
    {
        using var temp = TempWorkspace.Create();
        var tool = new WriteFileTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<WriteFileTool>.Instance);

        var result = await tool.WriteFile("../escape.txt", "x", overwrite: true, CancellationToken.None);

        await Assert.That(result).Contains("outside the agent workspace");
    }
}
