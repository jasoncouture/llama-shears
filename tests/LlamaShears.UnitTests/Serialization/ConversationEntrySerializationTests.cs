using System.Text.Json;
using LlamaShears.Provider.Abstractions;

namespace LlamaShears.UnitTests.Serialization;

public sealed class ConversationEntrySerializationTests
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    [Test]
    public async Task ModelTurnRoundTripsViaIConversationEntry()
    {
        var original = new ModelTurn(
            ModelRole.User,
            "hello",
            new DateTimeOffset(2026, 4, 29, 20, 0, 0, TimeSpan.Zero));

        var json = JsonSerializer.Serialize<IConversationEntry>(original, _options);
        var roundTripped = JsonSerializer.Deserialize<IConversationEntry>(json, _options);

        await Assert.That(roundTripped).IsEqualTo(original);
    }

    [Test]
    public async Task ModelTurnSerializesWithKindDiscriminator()
    {
        var turn = new ModelTurn(
            ModelRole.Assistant,
            "hi",
            new DateTimeOffset(2026, 4, 29, 20, 0, 0, TimeSpan.Zero));

        var json = JsonSerializer.Serialize<IConversationEntry>(turn, _options);

        await Assert.That(json).Contains("\"kind\":\"turn\"");
    }

    [Test]
    public async Task DeserializeWithUnknownKindThrows()
    {
        const string json = """
            {"kind":"reflection","content":"…"}
            """;

        await Assert.That(() => JsonSerializer.Deserialize<IConversationEntry>(json, _options))
            .Throws<JsonException>();
    }
}
