using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Paths;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;

public sealed class ReadFileToolTests
{
    [Test]
    public async Task ReadsWholeFileAndReportsEndOfFile()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "alpha\nbeta\ngamma");
        var tool = CreateTool(temp);

        var result = await tool.ReadFile("a.txt", startLine: 1, CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Content).IsEqualTo("alpha\nbeta\ngamma");
        await Assert.That(result.StartLine).IsEqualTo(1);
        await Assert.That(result.EndLine).IsEqualTo(3);
        await Assert.That(result.LinesReturned).IsEqualTo(3);
        await Assert.That(result.EndOfFile).IsTrue();
        await Assert.That(result.NextStartLine).IsNull();
    }

    [Test]
    public async Task StartLineSkipsLeadingLinesAndReportsRangeAccurately()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "one\ntwo\nthree\nfour\nfive");
        var tool = CreateTool(temp);

        var result = await tool.ReadFile("a.txt", startLine: 3, CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Content).IsEqualTo("three\nfour\nfive");
        await Assert.That(result.StartLine).IsEqualTo(3);
        await Assert.That(result.EndLine).IsEqualTo(5);
        await Assert.That(result.EndOfFile).IsTrue();
        await Assert.That(result.NextStartLine).IsNull();
    }

    [Test]
    public async Task TruncationReportsLineRangeAndNextStartLine()
    {
        using var temp = TempWorkspace.Create();
        var content = string.Join('\n', Enumerable.Range(1, 250).Select(i => $"L{i}"));
        await File.WriteAllTextAsync(temp.PathOf("big.txt"), content);
        var tool = CreateTool(temp);

        var result = await tool.ReadFile("big.txt", startLine: 1, CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.StartLine).IsEqualTo(1);
        await Assert.That(result.EndLine).IsEqualTo(100);
        await Assert.That(result.EndOfFile).IsFalse();
        await Assert.That(result.NextStartLine).IsEqualTo(101);
        await Assert.That(result.LinesReturned).IsEqualTo(100);
    }

    [Test]
    public async Task SecondCallResumesFromReportedStartLine()
    {
        using var temp = TempWorkspace.Create();
        var content = string.Join('\n', Enumerable.Range(1, 250).Select(i => $"L{i}"));
        await File.WriteAllTextAsync(temp.PathOf("big.txt"), content);
        var tool = CreateTool(temp);

        var second = await tool.ReadFile("big.txt", startLine: 101, CancellationToken.None);

        await Assert.That(second.Error).IsNull();
        await Assert.That(second.Content).StartsWith("L101\n");
        await Assert.That(second.StartLine).IsEqualTo(101);
        await Assert.That(second.EndLine).IsEqualTo(200);
        await Assert.That(second.EndOfFile).IsFalse();
        await Assert.That(second.NextStartLine).IsEqualTo(201);
    }

    [Test]
    public async Task ThirdCallReachesEndOfFileAndReportsRange()
    {
        using var temp = TempWorkspace.Create();
        var content = string.Join('\n', Enumerable.Range(1, 250).Select(i => $"L{i}"));
        await File.WriteAllTextAsync(temp.PathOf("big.txt"), content);
        var tool = CreateTool(temp);

        var tail = await tool.ReadFile("big.txt", startLine: 201, CancellationToken.None);

        await Assert.That(tail.Error).IsNull();
        await Assert.That(tail.Content).StartsWith("L201\n");
        await Assert.That(tail.StartLine).IsEqualTo(201);
        await Assert.That(tail.EndLine).IsEqualTo(250);
        await Assert.That(tail.EndOfFile).IsTrue();
        await Assert.That(tail.NextStartLine).IsNull();
    }

    [Test]
    public async Task StartLinePastEndOfFileReportsEmptyRange()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "one\ntwo\nthree");
        var tool = CreateTool(temp);

        var result = await tool.ReadFile("a.txt", startLine: 100, CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.LinesReturned).IsEqualTo(0);
        await Assert.That(result.Content).IsEqualTo(string.Empty);
        await Assert.That(result.EndOfFile).IsTrue();
        await Assert.That(result.NextStartLine).IsNull();
    }

    [Test]
    public async Task MissingFileReportsErrorAndEmptyContent()
    {
        using var temp = TempWorkspace.Create();
        var tool = CreateTool(temp);

        var result = await tool.ReadFile("missing.txt", startLine: 1, CancellationToken.None);

        await Assert.That(result.Error).IsNotNull().And.Contains("File not found");
        await Assert.That(result.Content).IsEqualTo(string.Empty);
        await Assert.That(result.LinesReturned).IsEqualTo(0);
    }

    private static ReadFileTool CreateTool(TempWorkspace temp)
        => new(
            new StubAgentWorkspaceLocator(temp.Workspace),
            new PathExpander(),
            TestFileProtectionPolicies.AllowAll,
            NullLogger<ReadFileTool>.Instance);
}
