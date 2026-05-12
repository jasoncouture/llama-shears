using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Paths;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;

public sealed class ReadFileToolTests
{
    [Test]
    public async Task ReadsWholeFileAndAppendsEndOfFileRange()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "alpha\nbeta\ngamma");
        var tool = CreateTool(temp);

        var result = await tool.ReadFile("a.txt", startLine: 1, CancellationToken.None);

        await Assert.That(result).StartsWith("alpha\nbeta\ngamma\n");
        await Assert.That(result).Contains("[lines 1-3; end of file.]");
        await Assert.That(result).DoesNotContain("truncated");
    }

    [Test]
    public async Task StartLineSkipsLeadingLinesAndReportsRangeAccurately()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "one\ntwo\nthree\nfour\nfive");
        var tool = CreateTool(temp);

        var result = await tool.ReadFile("a.txt", startLine: 3, CancellationToken.None);

        await Assert.That(result).StartsWith("three\nfour\nfive\n");
        await Assert.That(result).Contains("[lines 3-5; end of file.]");
    }

    [Test]
    public async Task TruncationMarkerReportsLineRangeAndNextStartLine()
    {
        using var temp = TempWorkspace.Create();
        var content = string.Join('\n', Enumerable.Range(1, 250).Select(i => $"L{i}"));
        await File.WriteAllTextAsync(temp.PathOf("big.txt"), content);
        var tool = CreateTool(temp);

        var result = await tool.ReadFile("big.txt", startLine: 1, CancellationToken.None);

        await Assert.That(result).Contains("truncated, response budget reached.");
        await Assert.That(result).Contains("[lines 1-100; truncated");
        await Assert.That(result).Contains("Re-call with startLine=101");
    }

    [Test]
    public async Task SecondCallResumesFromReportedStartLine()
    {
        using var temp = TempWorkspace.Create();
        var content = string.Join('\n', Enumerable.Range(1, 250).Select(i => $"L{i}"));
        await File.WriteAllTextAsync(temp.PathOf("big.txt"), content);
        var tool = CreateTool(temp);

        var second = await tool.ReadFile("big.txt", startLine: 101, CancellationToken.None);

        await Assert.That(second).StartsWith("L101\n");
        await Assert.That(second).Contains("[lines 101-200; truncated");
        await Assert.That(second).Contains("Re-call with startLine=201");
    }

    [Test]
    public async Task ThirdCallReachesEndOfFileAndReportsRange()
    {
        using var temp = TempWorkspace.Create();
        var content = string.Join('\n', Enumerable.Range(1, 250).Select(i => $"L{i}"));
        await File.WriteAllTextAsync(temp.PathOf("big.txt"), content);
        var tool = CreateTool(temp);

        var tail = await tool.ReadFile("big.txt", startLine: 201, CancellationToken.None);

        await Assert.That(tail).StartsWith("L201\n");
        await Assert.That(tail).Contains("[lines 201-250; end of file.]");
    }

    [Test]
    public async Task StartLinePastEndOfFileReportsEmptyRangeWithLineCountHint()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "one\ntwo\nthree");
        var tool = CreateTool(temp);

        var result = await tool.ReadFile("a.txt", startLine: 100, CancellationToken.None);

        await Assert.That(result).Contains("[empty range; file has fewer than 100 lines.]");
    }

    private static ReadFileTool CreateTool(TempWorkspace temp)
        => new(
            new StubAgentWorkspaceLocator(temp.Workspace),
            new PathExpander(),
            TestFileProtectionPolicies.AllowAll,
            NullLogger<ReadFileTool>.Instance);
}
