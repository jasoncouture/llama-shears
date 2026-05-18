using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

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
        var tool = new GrepTool(new StubAgentWorkspaceLocator(temp.Workspace), Substitute.For<IFileProtectionPolicy>(), NullLogger<GrepTool>.Instance);

        var result = await tool.Grep(
            "needle",
            pathGlob: "**/*.txt",
            caseInsensitive: false,
            maxMatches: 200,
            CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.MatchCount).IsEqualTo(1);
        await Assert.That(result.Matches.Length).IsEqualTo(1);
        var hit = result.Matches[0];
        await Assert.That(hit.Path).IsEqualTo("src/a.txt");
        await Assert.That(hit.Line).IsEqualTo(2);
        await Assert.That(hit.Column).IsEqualTo(6);
        await Assert.That(hit.Text).IsEqualTo("beta needle gamma");
    }

    [Test]
    public async Task RespectsCaseInsensitiveFlag()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "Hello World\n");
        var tool = new GrepTool(new StubAgentWorkspaceLocator(temp.Workspace), Substitute.For<IFileProtectionPolicy>(), NullLogger<GrepTool>.Instance);

        var result = await tool.Grep(
            "hello",
            pathGlob: "**/*.txt",
            caseInsensitive: true,
            maxMatches: 200,
            CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.MatchCount).IsEqualTo(1);
        var hit = result.Matches[0];
        await Assert.That(hit.Path).IsEqualTo("a.txt");
        await Assert.That(hit.Line).IsEqualTo(1);
        await Assert.That(hit.Column).IsEqualTo(1);
        await Assert.That(hit.Text).IsEqualTo("Hello World");
    }

    [Test]
    public async Task ReportsZeroMatchesWhenNothingMatches()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "nothing\n");
        var tool = new GrepTool(new StubAgentWorkspaceLocator(temp.Workspace), Substitute.For<IFileProtectionPolicy>(), NullLogger<GrepTool>.Instance);

        var result = await tool.Grep(
            "absent",
            pathGlob: "**/*.txt",
            caseInsensitive: false,
            maxMatches: 200,
            CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.MatchCount).IsEqualTo(0);
        await Assert.That(result.Matches.IsEmpty).IsTrue();
    }

    [Test]
    public async Task RestrictsScanToFilesMatchingTheGlob()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.cs"), "needle\n");
        await File.WriteAllTextAsync(temp.PathOf("b.txt"), "needle\n");
        var tool = new GrepTool(new StubAgentWorkspaceLocator(temp.Workspace), Substitute.For<IFileProtectionPolicy>(), NullLogger<GrepTool>.Instance);

        var result = await tool.Grep(
            "needle",
            pathGlob: "**/*.cs",
            caseInsensitive: false,
            maxMatches: 200,
            CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.MatchCount).IsEqualTo(1);
        await Assert.That(result.Matches[0].Path).IsEqualTo("a.cs");
    }
}
