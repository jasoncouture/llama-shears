using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Provider;

public sealed class CompositeIdentityTests
{
    [Test]
    public async Task ToStringReturnsProviderSlashModel()
    {
        var identity = new CompositeIdentity("ollama", "llama3");

        await Assert.That(identity.ToString()).IsEqualTo("ollama/llama3");
    }

    [Test]
    public async Task ImplicitToStringReturnsProviderSlashModel()
    {
        var identity = new CompositeIdentity("ollama", "llama3");

        string? rendered = identity;

        await Assert.That(rendered).IsEqualTo("ollama/llama3");
    }

    [Test]
    public async Task ImplicitToStringMapsNullIdentityToNull()
    {
        CompositeIdentity? identity = null;

        string? rendered = identity;

        await Assert.That(rendered).IsNull();
    }

    [Test]
    public async Task ExplicitFromStringRoundTripsThroughToString()
    {
        var original = new CompositeIdentity("ollama", "owner/repo:tag");

        var parsed = (CompositeIdentity?)original.ToString();

        await Assert.That(parsed).IsEqualTo(original);
    }

    [Test]
    public async Task ExplicitFromStringMapsNullToNull()
    {
        string? value = null;

        var parsed = (CompositeIdentity?)value;

        await Assert.That(parsed).IsNull();
    }

    [Test]
    public async Task ExplicitFromStringThrowsOnMissingSeparator()
    {
        await Assert.That(() => _ = (CompositeIdentity?)"no-slash-here")
            .Throws<FormatException>();
    }

    [Test]
    public async Task TryParseSplitsOnFirstSlashOnly()
    {
        var ok = CompositeIdentity.TryParse("ollama/owner/repo:tag", out var identity);

        await Assert.That(ok).IsTrue();
        await Assert.That(identity).IsNotNull();
        await Assert.That(identity!.Provider).IsEqualTo("ollama");
        await Assert.That(identity.Model).IsEqualTo("owner/repo:tag");
    }

    [Test]
    public async Task TryParseReturnsFalseOnMissingSeparator()
    {
        var ok = CompositeIdentity.TryParse("ollama", out var identity);

        await Assert.That(ok).IsFalse();
        await Assert.That(identity).IsNull();
    }
}
