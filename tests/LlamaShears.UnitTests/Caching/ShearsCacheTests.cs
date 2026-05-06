using LlamaShears.Core.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace LlamaShears.UnitTests.Caching;

public sealed class ShearsCacheTests
{
    [Test]
    public async Task TryGetReturnsAbsentForMissingKey()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new ShearsCache<OwnerA>(memory);

        var result = cache.TryGet<Payload>("nope");

        await Assert.That(result.Present).IsFalse();
        await Assert.That(result.TypeMismatch).IsFalse();
        await Assert.That(result.Value).IsNull();
    }

    [Test]
    public async Task SetThenTryGetReturnsTheCachedValue()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new ShearsCache<OwnerA>(memory);
        var payload = new Payload("hello");

        cache.Set("k1", payload, TimeSpan.FromMinutes(5));
        var result = cache.TryGet<Payload>("k1");

        await Assert.That(result.Present).IsTrue();
        await Assert.That(result.TypeMismatch).IsFalse();
        await Assert.That(result.Value).IsSameReferenceAs(payload);
    }

    [Test]
    public async Task TryGetWithWrongTypeReportsTypeMismatch()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new ShearsCache<OwnerA>(memory);

        cache.Set("k1", new Payload("hi"), TimeSpan.FromMinutes(5));
        var result = cache.TryGet<OtherPayload>("k1");

        await Assert.That(result.Present).IsTrue();
        await Assert.That(result.TypeMismatch).IsTrue();
        await Assert.That(result.Value).IsNull();
    }

    [Test]
    public async Task InvalidateRemovesTheEntry()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new ShearsCache<OwnerA>(memory);

        cache.Set("k1", new Payload("hi"), TimeSpan.FromMinutes(5));
        cache.Invalidate("k1");
        var result = cache.TryGet<Payload>("k1");

        await Assert.That(result.Present).IsFalse();
    }

    [Test]
    public async Task DifferentOwnersDoNotCollideOnTheSameKeyString()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var ownerA = new ShearsCache<OwnerA>(memory);
        var ownerB = new ShearsCache<OwnerB>(memory);

        ownerA.Set("shared", new Payload("a"), TimeSpan.FromMinutes(5));
        ownerB.Set("shared", new Payload("b"), TimeSpan.FromMinutes(5));

        var fromA = ownerA.TryGet<Payload>("shared");
        var fromB = ownerB.TryGet<Payload>("shared");

        await Assert.That(fromA.Value!.Text).IsEqualTo("a");
        await Assert.That(fromB.Value!.Text).IsEqualTo("b");
    }

    [Test]
    public async Task SetReplacesExistingEntry()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new ShearsCache<OwnerA>(memory);

        cache.Set("k1", new Payload("first"), TimeSpan.FromMinutes(5));
        cache.Set("k1", new Payload("second"), TimeSpan.FromMinutes(5));
        var result = cache.TryGet<Payload>("k1");

        await Assert.That(result.Value!.Text).IsEqualTo("second");
    }

    [Test]
    public async Task SetWithZeroTimeToLiveThrows()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new ShearsCache<OwnerA>(memory);

        await Assert.That(() => cache.Set("k1", new Payload("x"), TimeSpan.Zero))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task SetWithNegativeTimeToLiveThrows()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new ShearsCache<OwnerA>(memory);

        await Assert.That(() => cache.Set("k1", new Payload("x"), TimeSpan.FromMinutes(-1)))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ConstructorThrowsForNullMemoryCache()
    {
        await Assert.That(() => new ShearsCache<OwnerA>(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TryGetWithBlankKeyThrows()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new ShearsCache<OwnerA>(memory);

        await Assert.That(() => cache.TryGet<Payload>("   "))
            .Throws<ArgumentException>();
    }

    private sealed class OwnerA;

    private sealed class OwnerB;

    private sealed record Payload(string Text);

    private sealed record OtherPayload(int Number);
}
