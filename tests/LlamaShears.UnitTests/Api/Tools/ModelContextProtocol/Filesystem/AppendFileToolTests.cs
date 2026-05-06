using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;

public sealed class AppendFileToolTests
{
    [Test]
    public async Task AppendsToExistingFile()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("log.txt"), "first\n");
        var tool = new AppendFileTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<AppendFileTool>.Instance);

        var result = await tool.AppendFile("log.txt", "second\n", CancellationToken.None);

        await Assert.That(result).Contains("Appended");
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("log.txt"))).IsEqualTo("first\nsecond\n");
    }

    [Test]
    public async Task CreatesFileWhenMissing()
    {
        using var temp = TempWorkspace.Create();
        var tool = new AppendFileTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<AppendFileTool>.Instance);

        var result = await tool.AppendFile("nested/log.txt", "hello", CancellationToken.None);

        await Assert.That(result).Contains("Appended");
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("nested", "log.txt"))).IsEqualTo("hello");
    }

    [Test]
    public async Task RefusesAppendIntoSystemSubfolder()
    {
        using var temp = TempWorkspace.Create();
        var tool = new AppendFileTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<AppendFileTool>.Instance);

        var result = await tool.AppendFile("system/notes.md", "hi", CancellationToken.None);

        await Assert.That(result).Contains("'system/'");
    }
}
