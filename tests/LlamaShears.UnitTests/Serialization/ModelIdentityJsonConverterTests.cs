using System.Text.Json;
using LlamaShears.Provider.Abstractions;

namespace LlamaShears.UnitTests.Serialization;

public sealed class ModelIdentityJsonConverterTests
{
    [Test]
    public async Task Serializes_to_provider_slash_model()
    {
        var identity = new ModelIdentity("OLLAMA", "gemma4:26b");

        var json = JsonSerializer.Serialize(identity);

        await Assert.That(json).IsEqualTo("\"OLLAMA/gemma4:26b\"");
    }

    [Test]
    public async Task Deserializes_provider_slash_model()
    {
        var identity = JsonSerializer.Deserialize<ModelIdentity>("\"OLLAMA/gemma4:26b\"");

        await Assert.That(identity).IsNotNull();
        await Assert.That(identity!.Provider).IsEqualTo("OLLAMA");
        await Assert.That(identity.Model).IsEqualTo("gemma4:26b");
    }

    [Test]
    public async Task Splits_on_first_slash_only()
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
    public async Task Round_trips_through_serialize_and_deserialize()
    {
        var original = new ModelIdentity("OLLAMA", "owner/repo:tag");

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<ModelIdentity>(json);

        await Assert.That(roundTripped).IsEqualTo(original);
    }

    [Test]
    public async Task Null_token_deserializes_to_null()
    {
        var identity = JsonSerializer.Deserialize<ModelIdentity?>("null");

        await Assert.That(identity).IsNull();
    }

    [Test]
    public async Task Non_string_token_throws()
    {
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("123"))
            .Throws<JsonException>();
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("{}"))
            .Throws<JsonException>();
    }

    [Test]
    public async Task Empty_string_throws()
    {
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("\"\""))
            .Throws<JsonException>();
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("\"   \""))
            .Throws<JsonException>();
    }

    [Test]
    public async Task String_without_slash_throws()
    {
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("\"OLLAMA\""))
            .Throws<JsonException>();
    }

    [Test]
    public async Task Slash_with_empty_provider_or_model_throws()
    {
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("\"/model\""))
            .Throws<JsonException>();
        await Assert.That(() => JsonSerializer.Deserialize<ModelIdentity>("\"OLLAMA/\""))
            .Throws<JsonException>();
    }
}
