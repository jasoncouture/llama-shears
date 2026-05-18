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
        var before = DateTimeOffset.Now.AddSeconds(-5);
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "alpha\nbeta\ngamma");
        var tool = CreateTool(temp);

        var result = await tool.ReadFile("a.txt", startLine: 1, CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Content).IsEqualTo("alpha\nbeta\ngamma");
        await Assert.That(result.StartLine).IsEqualTo(1);
        await Assert.That(result.EndLine).IsEqualTo(3);
        await Assert.That(result.LinesReturned).IsEqualTo(3);
        await Assert.That(result.EndOfFile).IsTrue();
        await Assert.That(result.CreatedAt).IsNotNull();
        await Assert.That(result.ModifiedAt).IsNotNull();
        await Assert.That(result.ModifiedAt!.Value).IsGreaterThanOrEqualTo(before);
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
    }

    [Test]
    public async Task LargeFileTruncatesAndReportsContinuableRange()
    {
        using var temp = TempWorkspace.Create();
        const int totalLines = 5000;
        var padding = new string('x', 256);
        var content = string.Join('\n', Enumerable.Range(1, totalLines).Select(i => $"L{i:00000}{padding}"));
        await File.WriteAllTextAsync(temp.PathOf("big.txt"), content);
        var tool = CreateTool(temp);

        var first = await tool.ReadFile("big.txt", startLine: 1, CancellationToken.None);

        await Assert.That(first.Error).IsNull();
        await Assert.That(first.StartLine).IsEqualTo(1);
        await Assert.That(first.EndOfFile).IsFalse();
        await Assert.That(first.LinesReturned).IsGreaterThan(0);
        await Assert.That(first.EndLine).IsGreaterThanOrEqualTo(first.StartLine);
        await Assert.That(first.Content).StartsWith("L00001");
    }

    [Test]
    public async Task ResumeWithEndLinePlusOneEventuallyReachesEndOfFile()
    {
        using var temp = TempWorkspace.Create();
        const int totalLines = 5000;
        var padding = new string('x', 256);
        var content = string.Join('\n', Enumerable.Range(1, totalLines).Select(i => $"L{i:00000}{padding}"));
        await File.WriteAllTextAsync(temp.PathOf("big.txt"), content);
        var tool = CreateTool(temp);

        var next = 1;
        var iterations = 0;
        FileReadResult result;
        do
        {
            result = await tool.ReadFile("big.txt", startLine: next, CancellationToken.None);
            await Assert.That(result.Error).IsNull();
            await Assert.That(result.StartLine).IsEqualTo(next);
            await Assert.That(result.LinesReturned).IsGreaterThan(0);
            next = result.EndLine + 1;
            iterations++;
            await Assert.That(iterations).IsLessThanOrEqualTo(totalLines);
        } while (!result.EndOfFile);

        await Assert.That(result.EndLine).IsEqualTo(totalLines);
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
        await Assert.That(result.CreatedAt).IsNull();
        await Assert.That(result.ModifiedAt).IsNull();
    }

    private static ReadFileTool CreateTool(TempWorkspace temp)
        => new(
            new StubAgentWorkspaceLocator(temp.Workspace),
            new PathExpander(),
            TestFileProtectionPolicies.AllowAll,
            NullLogger<ReadFileTool>.Instance);
}
