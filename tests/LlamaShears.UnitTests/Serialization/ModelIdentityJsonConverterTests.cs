using System.Text.Json;
using LlamaShears.Provider.Abstractions;

namespace LlamaShears.UnitTests.Serialization;

public sealed class ModelIdentityJsonConverterTests
{
    [Test]
    public async Task SerializesToProviderSlashModel()
    {
        var identity = new ModelIdentity("OLLAMA", "gemma4:26b");

        var json = JsonSerializer.Serialize(identity);

        await Assert.That(json).IsEqualTo("\"OLLAMA/gemma4:26b\"");
    }

    [Test]
    public async Task DeserializesProviderSlashModel()
    {
        var identity = JsonSerializer.Deserialize<ModelIdentity>("\"OLLAMA/gemma4:26b\"");

        await Assert.That(identity).IsNotNull();
        await Assert.That(identity!.Provider).IsEqualTo("OLLAMA");
        await Assert.That(identity.Model).IsEqualTo("gemma4:26b");
    }

    [Test]
    public async Task SplitsOnFirstSlashOnly()
    {
        // Model ids may legitimately contain slashes (registry paths, etc.).
        // The provider name is constrained to the IProviderFactory.Name regex
        // (no slashes), so the first slash is unambiguously the separator.
        var identity = JsonSerializer.Deserialize<ModelIdentity>("\"OLLAMA/owner/repo:tag\"");

        await Assert.That(identity).IsNotNull();
        await Assert.That(identity!.Provider).IsEqualTo("OLLAMA");
        await Assert.That(identity.Model).IsEqualTo("owner/repo:tag");
    }

    [Test]
    public async Task RoundTripsThroughSerializeAndDeserialize()
    {
        var original = new ModelIdentity("OLLAMA", "owner/repo:tag");

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<ModelIdentity>(json);

        await Assert.That(roundTripped).IsEqualTo(original);
    }

    [Test]
    public async Task NullTokenDeserializesToNull()
    {
        var identity = JsonSerializer.Deserialize<ModelIdentity?>("null");

        await Assert.That(identity).IsNull();
    }

    [Test]
    public async Task NonStringTokenThrows()
    {
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("123"))
            .Throws<JsonException>();
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("{}"))
            .Throws<JsonException>();
    }

    [Test]
    public async Task EmptyStringThrows()
    {
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("\"\""))
            .Throws<JsonException>();
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("\"   \""))
            .Throws<JsonException>();
    }

    [Test]
    public async Task StringWithoutSlashThrows()
    {
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("\"OLLAMA\""))
            .Throws<JsonException>();
    }

    [Test]
    public async Task SlashWithEmptyProviderOrModelThrows()
    {
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("\"/model\""))
            .Throws<JsonException>();
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("\"OLLAMA/\""))
            .Throws<JsonException>();
    }
}
