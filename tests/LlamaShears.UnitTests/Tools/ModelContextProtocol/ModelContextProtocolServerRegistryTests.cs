using System.Collections.Immutable;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LlamaShears.UnitTests.Tools.ModelContextProtocol;

public sealed class ModelContextProtocolServerRegistryTests
{
    private static readonly Uri _internalUri = new("http://localhost:5125/mcp");

    [Test]
    public async Task ResolveWithNullWhitelistReturnsConfiguredAndInternal()
    {
        var registry = BuildRegistry(
            configured: new() { ["github"] = new Uri("https://gh/") },
            internalUri: _internalUri);

        var resolved = registry.Resolve(whitelist: null);

        await Assert.That(resolved.Keys.OrderBy(k => k, StringComparer.Ordinal))
            .IsEquivalentTo(["github", "llamashears"]);
    }

    [Test]
    public async Task ResolveWithNullWhitelistOmitsInternalWhenItsUriIsUnavailable()
    {
        var registry = BuildRegistry(
            configured: new() { ["github"] = new Uri("https://gh/") },
            internalUri: null);

        var resolved = registry.Resolve(whitelist: null);

        await Assert.That(resolved.Keys).IsEquivalentTo(["github"]);
    }

    [Test]
    public async Task ResolveWithEmptyWhitelistReturnsEmptyMapEvenWhenInternalIsAvailable()
    {
        var registry = BuildRegistry(
            configured: new() { ["github"] = new Uri("https://gh/") },
            internalUri: _internalUri);

        var resolved = registry.Resolve(whitelist: []);

        await Assert.That(resolved).IsEmpty();
    }

    [Test]
    public async Task ResolveExplicitWhitelistFiltersToTheNamedSubset()
    {
        var registry = BuildRegistry(
            configured: new()
            {
                ["github"] = new Uri("https://gh/"),
                ["linear"] = new Uri("https://lin/"),
            },
            internalUri: null);

        var resolved = registry.Resolve(whitelist: ["github"]);

        await Assert.That(resolved.Keys).IsEquivalentTo(["github"]);
        await Assert.That(resolved["github"]).IsEqualTo(new Uri("https://gh/"));
    }

    [Test]
    public async Task ResolveExplicitWhitelistCanIncludeInternalServerByName()
    {
        var registry = BuildRegistry(
            configured: [],
            internalUri: _internalUri);

        var resolved = registry.Resolve(whitelist: ["llamashears"]);

        await Assert.That(resolved.Keys).IsEquivalentTo(["llamashears"]);
        await Assert.That(resolved["llamashears"]).IsEqualTo(_internalUri);
    }

    [Test]
    public async Task ResolveDropsUnknownNamesFromWhitelist()
    {
        var registry = BuildRegistry(
            configured: new() { ["github"] = new Uri("https://gh/") },
            internalUri: null);

        var resolved = registry.Resolve(whitelist: ["github", "nope"]);

        await Assert.That(resolved.Keys).IsEquivalentTo(["github"]);
    }

    [Test]
    public async Task ResolveLooksUpWhitelistEntriesCaseInsensitively()
    {
        var registry = BuildRegistry(
            configured: new() { ["GitHub"] = new Uri("https://gh/") },
            internalUri: null);

        var resolved = registry.Resolve(whitelist: ["github"]);

        await Assert.That(resolved.Count).IsEqualTo(1);
        await Assert.That(resolved.ContainsKey("github")).IsTrue();
    }

    private static ModelContextProtocolServerRegistry BuildRegistry(
        Dictionary<string, Uri> configured,
        Uri? internalUri)
    {
        var options = new TestOptionsMonitor<ModelContextProtocolOptions>(
            new ModelContextProtocolOptions
            {
                Servers = new Dictionary<string, Uri>(configured, StringComparer.OrdinalIgnoreCase),
            });
        var internalServer = Substitute.For<IInternalModelContextProtocolServer>();
        internalServer.Uri.Returns(internalUri);
        return new ModelContextProtocolServerRegistry(
            options,
            internalServer,
            NullLogger<ModelContextProtocolServerRegistry>.Instance);
    }

    private sealed class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    {
        private readonly TOptions _value;

        public TestOptionsMonitor(TOptions value)
        {
            _value = value;
        }

        public TOptions CurrentValue => _value;

        public TOptions Get(string? name) => _value;

        public IDisposable OnChange(Action<TOptions, string?> listener) => new NoopDisposable();

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
