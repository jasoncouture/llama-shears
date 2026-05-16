using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LlamaShears.UnitTests.Tools.ModelContextProtocol;

public sealed class ModelContextProtocolServerRegistryTests
{
    private static readonly Uri _internalUri = new Uri("http://localhost:5125/mcp");

    [Test]
    public async Task ResolveWithNullWhitelistReturnsConfiguredAndInternal()
    {
        var registry = BuildRegistry(
            configured: new Dictionary<string, ModelContextProtocolServerOptions>
            {
                ["github"] = Server("https://gh/"),
            },
            internalUri: _internalUri);

        var resolved = registry.Resolve(whitelist: null);

        await Assert.That(resolved.Keys.OrderBy(k => k, StringComparer.Ordinal))
            .IsEquivalentTo(["github", "llamashears"]);
    }

    [Test]
    public async Task ResolveWithNullWhitelistOmitsInternalWhenItsUriIsUnavailable()
    {
        var registry = BuildRegistry(
            configured: new Dictionary<string, ModelContextProtocolServerOptions>
            {
                ["github"] = Server("https://gh/"),
            },
            internalUri: null);

        var resolved = registry.Resolve(whitelist: null);

        await Assert.That(resolved.Keys).IsEquivalentTo(["github"]);
    }

    [Test]
    public async Task ResolveWithEmptyWhitelistReturnsEmptyMapEvenWhenInternalIsAvailable()
    {
        var registry = BuildRegistry(
            configured: new Dictionary<string, ModelContextProtocolServerOptions>
            {
                ["github"] = Server("https://gh/"),
            },
            internalUri: _internalUri);

        var resolved = registry.Resolve(whitelist: []);

        await Assert.That(resolved).IsEmpty();
    }

    [Test]
    public async Task ResolveExplicitWhitelistFiltersToTheNamedSubset()
    {
        var registry = BuildRegistry(
            configured: new Dictionary<string, ModelContextProtocolServerOptions>
            {
                ["github"] = Server("https://gh/"),
                ["linear"] = Server("https://lin/"),
            },
            internalUri: null);

        var resolved = registry.Resolve(whitelist: ["github"]);

        await Assert.That(resolved.Keys).IsEquivalentTo(["github"]);
        await Assert.That(resolved["github"].Uri).IsEqualTo(new Uri("https://gh/"));
    }

    [Test]
    public async Task ResolveExplicitWhitelistCanIncludeInternalServerByName()
    {
        var registry = BuildRegistry(
            configured: [],
            internalUri: _internalUri);

        var resolved = registry.Resolve(whitelist: ["llamashears"]);

        await Assert.That(resolved.Keys).IsEquivalentTo(["llamashears"]);
        await Assert.That(resolved["llamashears"].Uri).IsEqualTo(_internalUri);
    }

    [Test]
    public async Task ResolveDropsUnknownNamesFromWhitelist()
    {
        var registry = BuildRegistry(
            configured: new Dictionary<string, ModelContextProtocolServerOptions>
            {
                ["github"] = Server("https://gh/"),
            },
            internalUri: null);

        var resolved = registry.Resolve(whitelist: ["github", "nope"]);

        await Assert.That(resolved.Keys).IsEquivalentTo(["github"]);
    }

    [Test]
    public async Task ResolveLooksUpWhitelistEntriesCaseInsensitively()
    {
        var registry = BuildRegistry(
            configured: new Dictionary<string, ModelContextProtocolServerOptions>
            {
                ["GitHub"] = Server("https://gh/"),
            },
            internalUri: null);

        var resolved = registry.Resolve(whitelist: ["github"]);

        await Assert.That(resolved.Count).IsEqualTo(1);
        await Assert.That(resolved.ContainsKey("github")).IsTrue();
    }

    [Test]
    public async Task ResolveCarriesConfiguredHeaders()
    {
        var registry = BuildRegistry(
            configured: new Dictionary<string, ModelContextProtocolServerOptions>
            {
                ["github"] = new ModelContextProtocolServerOptions
                {
                    Uri = new Uri("https://gh/"),
                    Headers = { ["Authorization"] = "Bearer abc", ["X-Trace"] = "1" },
                },
            },
            internalUri: null);

        var resolved = registry.Resolve(whitelist: ["github"]);

        await Assert.That(resolved["github"].Headers["Authorization"]).IsEqualTo("Bearer abc");
        await Assert.That(resolved["github"].Headers["X-Trace"]).IsEqualTo("1");
    }

    [Test]
    public async Task TryGetReturnsConfiguredEntry()
    {
        var registry = BuildRegistry(
            configured: new Dictionary<string, ModelContextProtocolServerOptions>
            {
                ["github"] = Server("https://gh/"),
            },
            internalUri: null);

        var entry = registry.TryGet("github");

        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.Uri).IsEqualTo(new Uri("https://gh/"));
    }

    [Test]
    public async Task TryGetReturnsInternalEntry()
    {
        var registry = BuildRegistry(
            configured: [],
            internalUri: _internalUri);

        var entry = registry.TryGet("llamashears");

        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.Uri).IsEqualTo(_internalUri);
    }

    [Test]
    public async Task TryGetReturnsNullForUnknownName()
    {
        var registry = BuildRegistry(
            configured: [],
            internalUri: null);

        var entry = registry.TryGet("nope");

        await Assert.That(entry).IsNull();
    }

    [Test]
    public async Task TryGetIsCaseInsensitive()
    {
        var registry = BuildRegistry(
            configured: new Dictionary<string, ModelContextProtocolServerOptions>
            {
                ["GitHub"] = Server("https://gh/"),
            },
            internalUri: null);

        var entry = registry.TryGet("github");

        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.Uri).IsEqualTo(new Uri("https://gh/"));
    }

    private static ModelContextProtocolServerOptions Server(string uri) =>
        new() { Uri = new Uri(uri) };

    private static ModelContextProtocolServerRegistry BuildRegistry(
        Dictionary<string, ModelContextProtocolServerOptions> configured,
        Uri? internalUri)
    {
        var options = new TestOptionsMonitor<ModelContextProtocolOptions>(
            new ModelContextProtocolOptions
            {
                Servers = new Dictionary<string, ModelContextProtocolServerOptions>(configured, StringComparer.OrdinalIgnoreCase),
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
