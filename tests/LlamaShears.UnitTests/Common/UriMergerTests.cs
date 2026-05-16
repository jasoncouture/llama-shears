using LlamaShears.Core.Common;

namespace LlamaShears.UnitTests.Common;

public sealed class UriMergerTests
{
    private readonly IUriMerger _merger = new UriMerger();

    [Test]
    public async Task ReturnsRootInstanceWhenOtherUriHasNoPathAndNoQuery()
    {
        var root = new Uri("https://host:8443/mcp");
        var other = new Uri("http://sentinel/");

        var merged = _merger.Merge(root, other);

        await Assert.That(merged).IsSameReferenceAs(root);
    }

    [Test]
    public async Task SwapsSchemeHostPortFromRootAndKeepsRootPathWhenTailIsRoot()
    {
        var root = new Uri("https://realhost:9000/mcp");
        var other = new Uri("http://sentinel/?token=abc");

        var merged = _merger.Merge(root, other);

        await Assert.That(merged.Scheme).IsEqualTo("https");
        await Assert.That(merged.Host).IsEqualTo("realhost");
        await Assert.That(merged.Port).IsEqualTo(9000);
        await Assert.That(merged.AbsolutePath).IsEqualTo("/mcp");
        await Assert.That(merged.Query).IsEqualTo("?token=abc");
    }

    [Test]
    public async Task AppendsOtherPathOntoRootBasePath()
    {
        var root = new Uri("https://host/mcp");
        var other = new Uri("http://sentinel/tools/list");

        var merged = _merger.Merge(root, other);

        await Assert.That(merged.AbsolutePath).IsEqualTo("/mcp/tools/list");
    }

    [Test]
    public async Task NormalizesTrailingSlashOnRootSoNoDoubleSlashAppears()
    {
        var root = new Uri("https://host/mcp/");
        var other = new Uri("http://sentinel/tools/list");

        var merged = _merger.Merge(root, other);

        await Assert.That(merged.AbsolutePath).IsEqualTo("/mcp/tools/list");
    }

    [Test]
    public async Task RootWithoutPathStillProducesLeadingSlashFromOther()
    {
        var root = new Uri("https://host");
        var other = new Uri("http://sentinel/tools/list");

        var merged = _merger.Merge(root, other);

        await Assert.That(merged.AbsolutePath).IsEqualTo("/tools/list");
    }

    [Test]
    public async Task CarriesOtherQueryWhenRootHasNoQuery()
    {
        var root = new Uri("https://host/mcp");
        var other = new Uri("http://sentinel/tools?cursor=abc");

        var merged = _merger.Merge(root, other);

        await Assert.That(merged.Query).IsEqualTo("?cursor=abc");
    }

    [Test]
    public async Task RootQueryWinsOnDuplicateKey()
    {
        var root = new Uri("https://host/mcp?token=ROOT");
        var other = new Uri("http://sentinel/tools?token=caller&cursor=abc");

        var merged = _merger.Merge(root, other);

        await Assert.That(merged.Query.TrimStart('?').Split('&'))
            .IsEquivalentTo(["cursor=abc", "token=ROOT"]);
    }

    [Test]
    public async Task MergesDisjointQueryKeys()
    {
        var root = new Uri("https://host/mcp?source=root");
        var other = new Uri("http://sentinel/tools?cursor=abc");

        var merged = _merger.Merge(root, other);

        await Assert.That(merged.Query.TrimStart('?').Split('&'))
            .IsEquivalentTo(["cursor=abc", "source=root"]);
    }

    [Test]
    public async Task TailRootPathWithOtherQueryDoesNotAppendSlash()
    {
        var root = new Uri("https://host/mcp");
        var other = new Uri("http://sentinel/?token=abc");

        var merged = _merger.Merge(root, other);

        await Assert.That(merged.AbsolutePath).IsEqualTo("/mcp");
        await Assert.That(merged.Query).IsEqualTo("?token=abc");
    }

    [Test]
    public async Task PreservesUserInfoFromRoot()
    {
        var root = new Uri("https://user:pass@host/mcp");
        var other = new Uri("http://sentinel/tools");

        var merged = _merger.Merge(root, other);

        await Assert.That(merged.UserInfo).IsEqualTo("user:pass");
        await Assert.That(merged.AbsolutePath).IsEqualTo("/mcp/tools");
    }

    [Test]
    public async Task NullRootThrows()
    {
        await Assert.That(() => _merger.Merge(rootUri: null!, otherUri: new Uri("http://sentinel/")))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task NullOtherThrows()
    {
        await Assert.That(() => _merger.Merge(rootUri: new Uri("https://host/"), otherUri: null!))
            .Throws<ArgumentNullException>();
    }
}
