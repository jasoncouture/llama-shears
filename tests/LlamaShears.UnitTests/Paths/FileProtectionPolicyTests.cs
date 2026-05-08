using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Paths;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests.Paths;

public sealed class FileProtectionPolicyTests
{
    private static readonly string _root = OperatingSystem.IsWindows() ? @"C:\ws" : "/ws";

    private static string InRoot(string relative)
        => Path.Combine(_root, relative);

    private static FileProtectionPolicy PolicyWith(params ProtectedFile[] rules)
    {
        var options = new FileProtectionPolicyOptions();
        foreach (var rule in rules)
        {
            options.Rules.Add(rule);
        }
        return new FileProtectionPolicy(Options.Create(options));
    }

    [Test]
    public async Task ReturnsNullWhenNoRulesMatch()
    {
        var policy = PolicyWith(new ProtectedFile(".git/**", ProtectionMode.Delete, FileType.Any));

        var match = policy.Match(_root, InRoot("notes.txt"), FileType.File, ProtectionMode.Delete);

        await Assert.That(match).IsNull();
    }

    [Test]
    public async Task MatchesGlobOnExactPath()
    {
        var rule = new ProtectedFile(".gitignore", ProtectionMode.Delete | ProtectionMode.Write, FileType.File, "gitignore");
        var policy = PolicyWith(rule);

        var match = policy.Match(_root, InRoot(".gitignore"), FileType.File, ProtectionMode.Write);

        await Assert.That(match).IsEqualTo(rule);
    }

    [Test]
    public async Task FiltersByMode()
    {
        var rule = new ProtectedFile(".gitignore", ProtectionMode.Write, FileType.File);
        var policy = PolicyWith(rule);

        var write = policy.Match(_root, InRoot(".gitignore"), FileType.File, ProtectionMode.Write);
        var delete = policy.Match(_root, InRoot(".gitignore"), FileType.File, ProtectionMode.Delete);

        await Assert.That(write).IsEqualTo(rule);
        await Assert.That(delete).IsNull();
    }

    [Test]
    public async Task FiltersByType()
    {
        var rule = new ProtectedFile("name", ProtectionMode.Delete, FileType.Directory);
        var policy = PolicyWith(rule);

        var asDir = policy.Match(_root, InRoot("name"), FileType.Directory, ProtectionMode.Delete);
        var asFile = policy.Match(_root, InRoot("name"), FileType.File, ProtectionMode.Delete);

        await Assert.That(asDir).IsEqualTo(rule);
        await Assert.That(asFile).IsNull();
    }

    [Test]
    public async Task RootStarMdDoesNotMatchSubdirectoryFile()
    {
        var rule = new ProtectedFile("*.md", ProtectionMode.Delete, FileType.File);
        var policy = PolicyWith(rule);

        var root = policy.Match(_root, InRoot("README.md"), FileType.File, ProtectionMode.Delete);
        var nested = policy.Match(_root, InRoot(Path.Combine("docs", "notes.md")), FileType.File, ProtectionMode.Delete);

        await Assert.That(root).IsEqualTo(rule);
        await Assert.That(nested).IsNull();
    }

    [Test]
    public async Task DoubleStarGitMatchesNested()
    {
        var rule = new ProtectedFile("**/.git/**", ProtectionMode.Delete, FileType.Any);
        var policy = PolicyWith(rule);

        var nested = policy.Match(_root, InRoot(Path.Combine("submodule", ".git", "HEAD")), FileType.File, ProtectionMode.Delete);

        await Assert.That(nested).IsEqualTo(rule);
    }

    [Test]
    public async Task GlobMatchIsCaseInsensitive()
    {
        var rule = new ProtectedFile(".gitignore", ProtectionMode.Write, FileType.File);
        var policy = PolicyWith(rule);

        var match = policy.Match(_root, InRoot(".GITIGNORE"), FileType.File, ProtectionMode.Write);

        await Assert.That(match).IsEqualTo(rule);
    }

    [Test]
    public async Task ReturnsFirstMatchingRule()
    {
        var first = new ProtectedFile(".gitignore", ProtectionMode.Write, FileType.File, "first");
        var second = new ProtectedFile(".gitignore", ProtectionMode.Write, FileType.File, "second");
        var policy = PolicyWith(first, second);

        var match = policy.Match(_root, InRoot(".gitignore"), FileType.File, ProtectionMode.Write);

        await Assert.That(match).IsEqualTo(first);
    }

    [Test]
    public async Task FileTypeAnyMatchesEachKind()
    {
        var rule = new ProtectedFile(".git/**", ProtectionMode.Delete, FileType.Any);
        var policy = PolicyWith(rule);

        var asFile = policy.Match(_root, InRoot(Path.Combine(".git", "HEAD")), FileType.File, ProtectionMode.Delete);
        var asDir = policy.Match(_root, InRoot(Path.Combine(".git", "objects")), FileType.Directory, ProtectionMode.Delete);
        var asSpecial = policy.Match(_root, InRoot(Path.Combine(".git", "sock")), FileType.Special, ProtectionMode.Delete);

        await Assert.That(asFile).IsEqualTo(rule);
        await Assert.That(asDir).IsEqualTo(rule);
        await Assert.That(asSpecial).IsEqualTo(rule);
    }

    [Test]
    public async Task ReturnsNullForNoneMode()
    {
        var rule = new ProtectedFile("anything", ProtectionMode.Delete, FileType.File);
        var policy = PolicyWith(rule);

        var match = policy.Match(_root, InRoot("anything"), FileType.File, ProtectionMode.None);

        await Assert.That(match).IsNull();
    }

    [Test]
    public async Task ReturnsNullForNoneType()
    {
        var rule = new ProtectedFile("anything", ProtectionMode.Delete, FileType.Any);
        var policy = PolicyWith(rule);

        var match = policy.Match(_root, InRoot("anything"), FileType.None, ProtectionMode.Delete);

        await Assert.That(match).IsNull();
    }

    [Test]
    public async Task MatchesAbsolutePathOutsideWorkspace()
    {
        var systemPath = OperatingSystem.IsWindows() ? @"C:\Windows\System32\config\SAM" : "/etc/shadow";
        var rule = new ProtectedFile(systemPath, ProtectionMode.Read | ProtectionMode.Write | ProtectionMode.Delete, FileType.Any, "system file");
        var policy = PolicyWith(rule);

        var match = policy.Match(_root, systemPath, FileType.File, ProtectionMode.Delete);

        await Assert.That(match).IsEqualTo(rule);
    }
}
