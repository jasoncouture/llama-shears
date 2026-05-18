using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Paths;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;

public sealed class RegexReplaceFileToolTests
{
    [Test]
    public async Task ReplacesMatchesAndReportsCount()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "foo bar foo");
        var tool = new RegexReplaceFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.AllowAll, NullLogger<RegexReplaceFileTool>.Instance);

        var result = await tool.RegexReplaceFile(
            "a.txt",
            "foo",
            "baz",
            caseInsensitive: false,
            multiline: true,
            maxReplacements: 0,
            CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Edited).IsTrue();
        await Assert.That(result.Replacements).IsEqualTo(2);
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("a.txt"))).IsEqualTo("baz bar baz");
    }

    [Test]
    public async Task RespectsMaxReplacementsCap()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "foo foo foo");
        var tool = new RegexReplaceFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.AllowAll, NullLogger<RegexReplaceFileTool>.Instance);

        var result = await tool.RegexReplaceFile(
            "a.txt",
            "foo",
            "X",
            caseInsensitive: false,
            multiline: true,
            maxReplacements: 1,
            CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Edited).IsTrue();
        await Assert.That(result.Replacements).IsEqualTo(1);
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("a.txt"))).IsEqualTo("X foo foo");
    }

    [Test]
    public async Task ReportsZeroReplacementsWhenPatternDoesNotMatch()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("a.txt"), "hello");
        var tool = new RegexReplaceFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.AllowAll, NullLogger<RegexReplaceFileTool>.Instance);

        var result = await tool.RegexReplaceFile(
            "a.txt",
            "world",
            "WORLD",
            caseInsensitive: false,
            multiline: true,
            maxReplacements: 0,
            CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Edited).IsFalse();
        await Assert.That(result.Replacements).IsEqualTo(0);
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("a.txt"))).IsEqualTo("hello");
    }

    [Test]
    public async Task RefusesEditingFilesInsideSystemSubfolder()
    {
        using var temp = TempWorkspace.Create();
        Directory.CreateDirectory(temp.PathOf("system"));
        await File.WriteAllTextAsync(temp.PathOf("system", "x.md"), "foo");
        var tool = new RegexReplaceFileTool(new StubAgentWorkspaceLocator(temp.Workspace), new PathExpander(), TestFileProtectionPolicies.AllowAll, NullLogger<RegexReplaceFileTool>.Instance);

        var result = await tool.RegexReplaceFile(
            "system/x.md",
            "foo",
            "bar",
            caseInsensitive: false,
            multiline: true,
            maxReplacements: 0,
            CancellationToken.None);

        await Assert.That(result.Edited).IsFalse();
        await Assert.That(result.Error).IsNotNull().And.Contains("'system/'");
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("system", "x.md"))).IsEqualTo("foo");
    }
}
