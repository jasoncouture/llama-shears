using System.Globalization;
using LlamaShears.Core.SystemPrompt;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class HardcodedSystemPromptProviderTests
{
    [Test]
    public async Task BuildReturnsNonEmptyBody()
    {
        var provider = new HardcodedSystemPromptProvider();

        var prompt = provider.Build("alice", DateTimeOffset.UtcNow);

        await Assert.That(prompt).IsNotEmpty();
    }

    [Test]
    public async Task BuildAppendsSuppliedTimestampInRoundTripFormat()
    {
        var provider = new HardcodedSystemPromptProvider();
        var now = new DateTimeOffset(2026, 4, 29, 12, 34, 56, TimeSpan.Zero);

        var prompt = provider.Build("alice", now);

        var expected = now.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        await Assert.That(prompt).EndsWith(expected);
    }

    [Test]
    public async Task BuildNormalizesNonUtcTimestampsToUtc()
    {
        var provider = new HardcodedSystemPromptProvider();
        var local = new DateTimeOffset(2026, 4, 29, 12, 0, 0, TimeSpan.FromHours(-5));

        var prompt = provider.Build("alice", local);

        var expected = local.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        await Assert.That(prompt).EndsWith(expected);
    }

    [Test]
    public async Task BuildThrowsForBlankAgentId()
    {
        var provider = new HardcodedSystemPromptProvider();

        await Assert.That(() => provider.Build("   ", DateTimeOffset.UtcNow))
            .Throws<ArgumentException>();
    }
}
