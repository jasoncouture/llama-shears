using System.Security.Cryptography;
using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Caching;
using LlamaShears.Core.Paths;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests;

public sealed class AgentConfigProviderTests
{
    [Test]
    public async Task ParsedConfigCarriesSha256OfTheRawFileBytes()
    {
        using var fixture = new Fixture();
        var bytes = await fixture.WriteAgentAsync("alpha", """
            { "model": { "id": "TEST/stub" } }
            """);

        var config = await fixture.Provider.GetConfigAsync("alpha", CancellationToken.None);

        await Assert.That(config).IsNotNull();
        await Assert.That(config!.Hash).IsEqualTo(Convert.ToHexString(SHA256.HashData(bytes)));
    }

    [Test]
    public async Task HashChangesWhenTheFileBytesChange()
    {
        using var fixture = new Fixture();
        await fixture.WriteAgentAsync("alpha", """
            { "model": { "id": "TEST/stub" } }
            """);

        var first = await fixture.Provider.GetConfigAsync("alpha", CancellationToken.None);

        await fixture.WriteAgentAsync("alpha", """
            { "model": { "id": "TEST/different" } }
            """);
        var second = await fixture.Provider.GetConfigAsync("alpha", CancellationToken.None);

        await Assert.That(first!.Hash).IsNotEqualTo(second!.Hash);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _root;
        private readonly MemoryCache _memory;
        private readonly FileParserCache<AgentConfigProvider> _cache;

        public Fixture()
        {
            _root = Path.Combine(Path.GetTempPath(), $"llamashears-agentconfig-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_root);

            var pathsOptions = Options.Create(new ShearsPathsOptions
            {
                DataRoot = Path.Combine(_root, "data"),
            });
            IShearsPaths paths = new ShearsPaths(pathsOptions);
            Directory.CreateDirectory(paths.GetPath(PathKind.Agents));

            _memory = new MemoryCache(new MemoryCacheOptions());
            IShearsCache<AgentConfigProvider> shears = new ShearsCache<AgentConfigProvider>(_memory);
            var fpcOptions = new TestOptionsMonitor<FileParserCacheOptions>(
                new FileParserCacheOptions { TimeToLive = TimeSpan.FromMinutes(10) });
            _cache = new FileParserCache<AgentConfigProvider>(shears, fpcOptions);

            Provider = new AgentConfigProvider(paths, _cache, NullLogger<AgentConfigProvider>.Instance);
            AgentsDir = paths.GetPath(PathKind.Agents);
        }

        public AgentConfigProvider Provider { get; }

        public string AgentsDir { get; }

        public async Task<byte[]> WriteAgentAsync(string id, string body)
        {
            var path = Path.Combine(AgentsDir, $"{id}.json");
            var bytes = System.Text.Encoding.UTF8.GetBytes(body);
            await File.WriteAllBytesAsync(path, bytes);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
            return bytes;
        }

        public void Dispose()
        {
            _cache.Dispose();
            _memory.Dispose();
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
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
