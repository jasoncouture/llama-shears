using System.Text.Json;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Serialization;

public sealed class CompositeIdentityJsonConverterTests
{
    [Test]
    public async Task SerializesToProviderSlashModel()
    {
        var identity = new CompositeIdentity("OLLAMA", "gemma4:26b");

        var json = JsonSerializer.Serialize(identity);

        await Assert.That(json).IsEqualTo("\"OLLAMA/gemma4:26b\"");
    }

    [Test]
    public async Task DeserializesProviderSlashModel()
    {
        var identity = JsonSerializer.Deserialize<CompositeIdentity>("\"OLLAMA/gemma4:26b\"");

        await Assert.That(identity).IsNotNull();
        await Assert.That(identity!.Provider).IsEqualTo("OLLAMA");
        await Assert.That(identity.Model).IsEqualTo("gemma4:26b");
    }

    [Test]
    public async Task SplitsOnFirstSlashOnly()
    {
        var identity = JsonSerializer.Deserialize<CompositeIdentity>("\"OLLAMA/owner/repo:tag\"");

        await Assert.That(identity).IsNotNull();
        await Assert.That(identity!.Provider).IsEqualTo("OLLAMA");
        await Assert.That(identity.Model).IsEqualTo("owner/repo:tag");
    }

    [Test]
    public async Task RoundTripsThroughSerializeAndDeserialize()
    {
        var original = new CompositeIdentity("OLLAMA", "owner/repo:tag");

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<CompositeIdentity>(json);

        await Assert.That(roundTripped).IsEqualTo(original);
    }

    [Test]
    public async Task NullTokenDeserializesToNull()
    {
        var identity = JsonSerializer.Deserialize<CompositeIdentity?>("null");

        await Assert.That(identity).IsNull();
    }

    [Test]
    public async Task NonStringTokenThrows()
    {
        await Assert.That(() => JsonSerializer.Deserialize<CompositeIdentity>("123"))
            .Throws<JsonException>();
        await Assert.That(() => JsonSerializer.Deserialize<CompositeIdentity>("{}"))
            .Throws<JsonException>();
    }

    [Test]
    public async Task EmptyStringThrows()
    {
        await Assert.That(() => JsonSerializer.Deserialize<CompositeIdentity>("\"\""))
            .Throws<JsonException>();
        await Assert.That(() => JsonSerializer.Deserialize<CompositeIdentity>("\"   \""))
            .Throws<JsonException>();
    }

    [Test]
    public async Task StringWithoutSlashThrows()
    {
        await Assert.That(() => JsonSerializer.Deserialize<CompositeIdentity>("\"OLLAMA\""))
            .Throws<JsonException>();
    }

    [Test]
    public async Task SlashWithEmptyProviderOrModelThrows()
    {
        await Assert.That(() => JsonSerializer.Deserialize<CompositeIdentity>("\"/model\""))
            .Throws<JsonException>();
        await Assert.That(() => JsonSerializer.Deserialize<CompositeIdentity>("\"OLLAMA/\""))
            .Throws<JsonException>();
    }
}
