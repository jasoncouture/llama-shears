using LlamaShears.Api.Tools.ModelContextProtocol.Discord;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Discord;

public sealed class DiscordWebhookUrlTests
{
    [Test]
    public async Task AcceptsCanonicalDiscordWebhookUrl()
    {
        var accepted = DiscordWebhookUrl.TryValidate(
            "https://discord.com/api/webhooks/123456789012345678/abcDEF._-token",
            out var uri,
            out var error);

        await Assert.That(accepted).IsTrue();
        await Assert.That(error).IsNull();
        await Assert.That(uri).IsNotNull();
        await Assert.That(uri!.Host).IsEqualTo("discord.com");
    }

    [Test]
    public async Task AcceptsLegacyDiscordAppHostAndVersionedPath()
    {
        var accepted = DiscordWebhookUrl.TryValidate(
            "https://discordapp.com/api/v10/webhooks/1/token",
            out var uri,
            out var error);

        await Assert.That(accepted).IsTrue();
        await Assert.That(error).IsNull();
        await Assert.That(uri!.Host).IsEqualTo("discordapp.com");
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task RejectsMissingUrl(string? value)
    {
        var accepted = DiscordWebhookUrl.TryValidate(value, out var uri, out var error);

        await Assert.That(accepted).IsFalse();
        await Assert.That(uri).IsNull();
        await Assert.That(error).Contains("required");
    }

    [Test]
    public async Task RejectsHttp()
    {
        var accepted = DiscordWebhookUrl.TryValidate(
            "http://discord.com/api/webhooks/1/token",
            out var uri,
            out var error);

        await Assert.That(accepted).IsFalse();
        await Assert.That(uri).IsNull();
        await Assert.That(error).Contains("https");
    }

    [Test]
    public async Task RejectsNonDiscordHost()
    {
        var accepted = DiscordWebhookUrl.TryValidate(
            "https://example.com/api/webhooks/1/token",
            out var uri,
            out var error);

        await Assert.That(accepted).IsFalse();
        await Assert.That(uri).IsNull();
        await Assert.That(error).Contains("discord.com");
    }

    [Test]
    public async Task RejectsQueryString()
    {
        var accepted = DiscordWebhookUrl.TryValidate(
            "https://discord.com/api/webhooks/1/token?wait=true",
            out var uri,
            out var error);

        await Assert.That(accepted).IsFalse();
        await Assert.That(uri).IsNull();
        await Assert.That(error).Contains("query");
    }

    [Test]
    public async Task RejectsNonWebhookPath()
    {
        var accepted = DiscordWebhookUrl.TryValidate(
            "https://discord.com/api/channels/1/messages",
            out var uri,
            out var error);

        await Assert.That(accepted).IsFalse();
        await Assert.That(uri).IsNull();
        await Assert.That(error).Contains("webhooks");
    }
}
