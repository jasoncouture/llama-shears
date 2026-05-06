using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;

public sealed class GrepToolTests
{
    [Test]
    public async Task ReturnsRelativePathLineAndColumnForEachMatch()
    {
        using var temp = TempWorkspace.Create();
        Directory.CreateDirectory(temp.PathOf("src"));
        await File.WriteAllTextAsync(temp.PathOf("src", "a.txt"), "alpha\nbeta needle gamma\n");
        await File.WriteAllTextAsync(temp.PathOf("src", "b.txt"), "no hits here\n");
        var tool = new GrepTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<GrepTool>.Instance);

        var result = await tool.Grep(
            "needle",
            pathGlob: "**/*.txt",
            caseInsensitive: false,
            maxMatches: 200,
            CancellationToken.None);

        await Assert.That(result).Contains("src/a.txt:2:6: beta needle gamma");
    }

    [Test]
    public async Task RespectsCaseInsensitiveFlag()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "Hello World\n");
        var tool = new GrepTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<GrepTool>.Instance);

        var result = await tool.Grep(
            "hello",
            pathGlob: "**/*.txt",
            caseInsensitive: true,
            maxMatches: 200,
            CancellationToken.None);

        await Assert.That(result).Contains("a.txt:1:1: Hello World");
    }

    [Test]
    public async Task ReportsNoMatchesWhenNothingMatches()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "nothing\n");
        var tool = new GrepTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<GrepTool>.Instance);

        var result = await tool.Grep(
            "absent",
            pathGlob: "**/*.txt",
            caseInsensitive: false,
            maxMatches: 200,
            CancellationToken.None);

        await Assert.That(result).Contains("No matches");
    }

    [Test]
    public async Task RestrictsScanToFilesMatchingTheGlob()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.cs"), "needle\n");
        await File.WriteAllTextAsync(temp.PathOf("b.txt"), "needle\n");
        var tool = new GrepTool(new StubAgentWorkspaceLocator(temp.Workspace), NullLogger<GrepTool>.Instance);

        var result = await tool.Grep(
            "needle",
            pathGlob: "**/*.cs",
            caseInsensitive: false,
            maxMatches: 200,
            CancellationToken.None);

        await Assert.That(result).Contains("a.cs");
        await Assert.That(result).DoesNotContain("b.txt");
    }
}
