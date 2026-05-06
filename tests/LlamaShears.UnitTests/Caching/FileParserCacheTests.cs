using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests.Caching;

public sealed class FileParserCacheTests
{
    [Test]
    public async Task ParserReceivesOpenStreamForExistingFile()
    {
        using var fixture = new Fixture();
        await File.WriteAllTextAsync(fixture.Path, "hello world");

        var calls = 0;
        var parsed = await fixture.Cache.GetOrParseAsync<Payload, int>(
            fixture.Path,
            state: 7,
            parser: async (stream, state, ct) =>
            {
                Interlocked.Increment(ref calls);
                using var reader = new StreamReader(stream!);
                return new Payload(await reader.ReadToEndAsync(ct), state);
            },
            cancellationToken: CancellationToken.None);

        await Assert.That(parsed!.Text).IsEqualTo("hello world");
        await Assert.That(parsed.State).IsEqualTo(7);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task ParserReceivesNullStreamWhenFileMissing()
    {
        using var fixture = new Fixture();

        Stream? observedStream = null;
        var parsed = await fixture.Cache.GetOrParseAsync<Payload, int>(
            fixture.Path,
            state: 0,
            parser: (stream, _, _) =>
            {
                observedStream = stream;
                return ValueTask.FromResult<Payload?>(null);
            },
            cancellationToken: CancellationToken.None);

        await Assert.That(observedStream).IsNull();
        await Assert.That(parsed).IsNull();
    }

    [Test]
    public async Task RepeatedCallsHitCacheWithoutInvokingParser()
    {
        using var fixture = new Fixture();
        await File.WriteAllTextAsync(fixture.Path, "first");

        var calls = 0;
        Func<Stream?, int, CancellationToken, ValueTask<Payload?>> parser =
            async (stream, state, ct) =>
            {
                Interlocked.Increment(ref calls);
                using var reader = new StreamReader(stream!);
                return new Payload(await reader.ReadToEndAsync(ct), state);
            };

        var first = await fixture.Cache.GetOrParseAsync(fixture.Path, 1, parser, CancellationToken.None);
        var second = await fixture.Cache.GetOrParseAsync(fixture.Path, 1, parser, CancellationToken.None);

        await Assert.That(calls).IsEqualTo(1);
        await Assert.That(second).IsSameReferenceAs(first);
    }

    [Test]
    public async Task FileMutationProducesFreshParseOnNextCall()
    {
        using var fixture = new Fixture();
        await File.WriteAllTextAsync(fixture.Path, "first");

        var calls = 0;
        Func<Stream?, int, CancellationToken, ValueTask<Payload?>> parser =
            async (stream, state, ct) =>
            {
                Interlocked.Increment(ref calls);
                using var reader = new StreamReader(stream!);
                return new Payload(await reader.ReadToEndAsync(ct), state);
            };

        var first = await fixture.Cache.GetOrParseAsync(fixture.Path, 1, parser, CancellationToken.None);

        File.SetLastWriteTimeUtc(fixture.Path, DateTime.UtcNow.AddSeconds(60));
        await File.WriteAllTextAsync(fixture.Path, "second-and-longer");
        File.SetLastWriteTimeUtc(fixture.Path, DateTime.UtcNow.AddSeconds(120));

        var second = await fixture.Cache.GetOrParseAsync(fixture.Path, 1, parser, CancellationToken.None);

        await Assert.That(calls).IsEqualTo(2);
        await Assert.That(second!.Text).IsEqualTo("second-and-longer");
        await Assert.That(first!.Text).IsEqualTo("first");
    }

    [Test]
    public async Task DifferentTItemTypesCacheIndependentlyForSamePath()
    {
        using var fixture = new Fixture();
        await File.WriteAllTextAsync(fixture.Path, "abc");

        var payloadCalls = 0;
        var otherCalls = 0;

        await fixture.Cache.GetOrParseAsync<Payload, int>(
            fixture.Path,
            state: 0,
            parser: (_, _, _) => { Interlocked.Increment(ref payloadCalls); return ValueTask.FromResult<Payload?>(new Payload("p", 0)); },
            cancellationToken: CancellationToken.None);

        await fixture.Cache.GetOrParseAsync<OtherPayload, int>(
            fixture.Path,
            state: 0,
            parser: (_, _, _) => { Interlocked.Increment(ref otherCalls); return ValueTask.FromResult<OtherPayload?>(new OtherPayload(42)); },
            cancellationToken: CancellationToken.None);

        await Assert.That(payloadCalls).IsEqualTo(1);
        await Assert.That(otherCalls).IsEqualTo(1);
    }

    [Test]
    public async Task TimeToLiveUpdateAppliesToFutureSetCalls()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var sharedCache = new Core.Caching.ShearsCache<OwnerA>(memory);
        var monitor = new MutableOptionsMonitor<FileParserCacheOptions>(
            new FileParserCacheOptions { TimeToLive = TimeSpan.FromMilliseconds(50) });
        using var cache = new FileParserCache<OwnerA>(sharedCache, monitor);
        using var fixture = new Fixture(cache);
        await File.WriteAllTextAsync(fixture.Path, "v1");

        var calls = 0;
        Func<Stream?, int, CancellationToken, ValueTask<Payload?>> parser =
            async (stream, state, ct) =>
            {
                Interlocked.Increment(ref calls);
                using var reader = new StreamReader(stream!);
                return new Payload(await reader.ReadToEndAsync(ct), state);
            };

        await fixture.Cache.GetOrParseAsync(fixture.Path, 0, parser, CancellationToken.None);
        await Task.Delay(150, CancellationToken.None);
        await fixture.Cache.GetOrParseAsync(fixture.Path, 0, parser, CancellationToken.None);
        await Assert.That(calls).IsEqualTo(2);

        monitor.Set(new FileParserCacheOptions { TimeToLive = TimeSpan.FromMinutes(30) });

        await fixture.Cache.GetOrParseAsync(fixture.Path, 0, parser, CancellationToken.None);
        await Task.Delay(150, CancellationToken.None);
        await fixture.Cache.GetOrParseAsync(fixture.Path, 0, parser, CancellationToken.None);

        await Assert.That(calls).IsEqualTo(3);
    }

    [Test]
    public async Task NonPositiveTimeToLiveUpdateIsIgnored()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var sharedCache = new Core.Caching.ShearsCache<OwnerA>(memory);
        var monitor = new MutableOptionsMonitor<FileParserCacheOptions>(
            new FileParserCacheOptions { TimeToLive = TimeSpan.FromMinutes(30) });
        using var cache = new FileParserCache<OwnerA>(sharedCache, monitor);
        using var fixture = new Fixture(cache);
        await File.WriteAllTextAsync(fixture.Path, "v1");

        monitor.Set(new FileParserCacheOptions { TimeToLive = TimeSpan.Zero });

        var calls = 0;
        Func<Stream?, int, CancellationToken, ValueTask<Payload?>> parser =
            async (stream, state, ct) =>
            {
                Interlocked.Increment(ref calls);
                using var reader = new StreamReader(stream!);
                return new Payload(await reader.ReadToEndAsync(ct), state);
            };

        await fixture.Cache.GetOrParseAsync(fixture.Path, 0, parser, CancellationToken.None);
        await fixture.Cache.GetOrParseAsync(fixture.Path, 0, parser, CancellationToken.None);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task BlankPathThrows(string? path)
    {
        using var fixture = new Fixture();

        await Assert.That(() => fixture.Cache.GetOrParseAsync<Payload, int>(
                path!,
                state: 0,
                parser: (_, _, _) => ValueTask.FromResult<Payload?>(null),
                cancellationToken: CancellationToken.None).AsTask())
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task NullParserThrows()
    {
        using var fixture = new Fixture();

        await Assert.That(() => fixture.Cache.GetOrParseAsync<Payload, int>(
                fixture.Path,
                state: 0,
                parser: null!,
                cancellationToken: CancellationToken.None).AsTask())
            .Throws<ArgumentNullException>();
    }

    private sealed class Fixture : IDisposable
    {
        private readonly MemoryCache? _ownedMemory;
        private readonly FileParserCache<OwnerA>? _ownedCache;

        public Fixture()
        {
            _ownedMemory = new MemoryCache(new MemoryCacheOptions());
            var shears = new Core.Caching.ShearsCache<OwnerA>(_ownedMemory);
            var monitor = new MutableOptionsMonitor<FileParserCacheOptions>(
                new FileParserCacheOptions { TimeToLive = TimeSpan.FromMinutes(10) });
            _ownedCache = new FileParserCache<OwnerA>(shears, monitor);
            Cache = _ownedCache;
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"llamashears-fpc-{Guid.NewGuid():N}.bin");
        }

        public Fixture(IFileParserCache<OwnerA> cache)
        {
            Cache = cache;
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"llamashears-fpc-{Guid.NewGuid():N}.bin");
        }

        public IFileParserCache<OwnerA> Cache { get; }

        public string Path { get; }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
            _ownedCache?.Dispose();
            _ownedMemory?.Dispose();
        }
    }

    private sealed class MutableOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    {
        private readonly List<Action<TOptions, string?>> _listeners = [];
        private TOptions _value;

        public MutableOptionsMonitor(TOptions initial)
        {
            _value = initial;
        }

        public TOptions CurrentValue => _value;

        public TOptions Get(string? name) => _value;

        public IDisposable OnChange(Action<TOptions, string?> listener)
        {
            _listeners.Add(listener);
            return new Subscription(() => _listeners.Remove(listener));
        }

        public void Set(TOptions value)
        {
            _value = value;
            foreach (var listener in _listeners.ToArray())
            {
                listener(value, null);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action _onDispose;
            public Subscription(Action onDispose) => _onDispose = onDispose;
            public void Dispose() => _onDispose();
        }
    }

    private sealed class OwnerA;

    private sealed record Payload(string Text, int State);

    private sealed record OtherPayload(int Number);
}
