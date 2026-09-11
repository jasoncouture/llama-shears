using LlamaShears.Api.Tools.ModelContextProtocol.Discord;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Discord;

public sealed class DiscordWebhookOptionsValidatorTests
{
    [Test]
    public async Task EmptyWebhooksAreValid()
    {
        IValidateOptions<DiscordWebhookOptions> validator = new DiscordWebhookOptionsValidator();

        var result = validator.Validate(null, new DiscordWebhookOptions());

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task ValidWebhookPasses()
    {
        IValidateOptions<DiscordWebhookOptions> validator = new DiscordWebhookOptionsValidator();
        var options = new DiscordWebhookOptions
        {
            Webhooks = { ["alerts"] = "https://discord.com/api/webhooks/1/token" },
        };

        var result = validator.Validate(null, options);

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task InvalidWebhookFailsWithTheName()
    {
        IValidateOptions<DiscordWebhookOptions> validator = new DiscordWebhookOptionsValidator();
        var options = new DiscordWebhookOptions
        {
            Webhooks = { ["alerts"] = "https://example.com/hook" },
        };

        var result = validator.Validate(null, options);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("alerts");
        await Assert.That(result.FailureMessage).Contains("discord.com");
    }

    [Test]
    public async Task BlankWebhookNameFails()
    {
        IValidateOptions<DiscordWebhookOptions> validator = new DiscordWebhookOptionsValidator();
        var options = new DiscordWebhookOptions
        {
            Webhooks = { ["  "] = "https://discord.com/api/webhooks/1/token" },
        };

        var result = validator.Validate(null, options);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("names must be non-empty");
    }
}
