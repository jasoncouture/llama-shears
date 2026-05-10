using System.Collections.Immutable;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Provider;

public sealed class ModelConfigurationExtensionsTests
{
    private static ModelConfiguration Build(params (string Key, JsonElement Value)[] entries)
    {
        ImmutableDictionary<string, JsonElement>? parameters = null;
        if (entries.Length > 0)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, JsonElement>(StringComparer.Ordinal);
            foreach (var (key, value) in entries)
            {
                builder[key] = value;
            }
            parameters = builder.ToImmutable();
        }
        return new ModelConfiguration(new CompositeIdentity("test", "model"), Parameters: parameters);
    }

    private static JsonElement Json(string raw) => JsonSerializer.Deserialize<JsonElement>(raw);

    [Test]
    public async Task TryGetValueReturnsFalseWhenParametersNull()
    {
        var configuration = Build();

        var found = configuration.TryGetValue<string>("missing", out var value);

        await Assert.That(found).IsFalse();
        await Assert.That(value).IsNull();
    }

    [Test]
    public async Task TryGetValueReturnsFalseWhenKeyAbsent()
    {
        var configuration = Build(("other", Json("\"x\"")));

        var found = configuration.TryGetValue<string>("missing", out var value);

        await Assert.That(found).IsFalse();
        await Assert.That(value).IsNull();
    }

    [Test]
    public async Task TryGetValueReferenceTypeNullElementReturnsTrueWithNull()
    {
        var configuration = Build(("note", Json("null")));

        var found = configuration.TryGetValue<string>("note", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsNull();
    }

    [Test]
    public async Task TryGetValueNullableStructNullElementReturnsTrueWithNull()
    {
        var configuration = Build(("count", Json("null")));

        var found = configuration.TryGetValue<int?>("count", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsNull();
    }

    [Test]
    public async Task TryGetValueNullableTimeSpanNullElementReturnsTrueWithNull()
    {
        var configuration = Build(("keepAlive", Json("null")));

        var found = configuration.TryGetValue<TimeSpan?>("keepAlive", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsNull();
    }

    [Test]
    public async Task TryGetValueNonNullableStructNullElementReturnsFalse()
    {
        var configuration = Build(("count", Json("null")));

        var found = configuration.TryGetValue<int>("count", out var value);

        await Assert.That(found).IsFalse();
        await Assert.That(value).IsEqualTo(0);
    }

    [Test]
    public async Task TryGetValueDeserializesString()
    {
        var configuration = Build(("name", Json("\"alpha\"")));

        var found = configuration.TryGetValue<string>("name", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo("alpha");
    }

    [Test]
    public async Task TryGetValueDeserializesInt()
    {
        var configuration = Build(("count", Json("42")));

        var found = configuration.TryGetValue<int>("count", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task TryGetValueDeserializesTimeSpanFromString()
    {
        var configuration = Build(("keepAlive", Json("\"01:00:00\"")));
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var found = configuration.TryGetValue<TimeSpan>("keepAlive", out var value, jsonOptions);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo(TimeSpan.FromHours(1));
    }

    [Test]
    public async Task TryGetValueReturnsFalseWhenDeserializationFails()
    {
        var configuration = Build(("count", Json("\"not-a-number\"")));

        var found = configuration.TryGetValue<int>("count", out var value);

        await Assert.That(found).IsFalse();
        await Assert.That(value).IsEqualTo(0);
    }

    [Test]
    public async Task TryGetValueReferenceTypeDeserializationFailureReturnsFalse()
    {
        var configuration = Build(("payload", Json("\"not-an-object\"")));

        var found = configuration.TryGetValue<Sample>("payload", out var value);

        await Assert.That(found).IsFalse();
        await Assert.That(value).IsNull();
    }

    [Test]
    public async Task TryGetValueThrowsOnNullConfiguration()
    {
        ModelConfiguration? configuration = null;

        await Assert.That(() => configuration!.TryGetValue<string>("key", out _)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TryGetValueThrowsOnEmptyKey()
    {
        var configuration = Build();

        await Assert.That(() => configuration.TryGetValue<string>(string.Empty, out _)).Throws<ArgumentException>();
    }

    private sealed record Sample(string Name);
}
