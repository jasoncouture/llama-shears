using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Eventing;

namespace LlamaShears.UnitTests.Eventing;

public sealed class PatternMatcherTests
{
    [Test]
    public async Task ExactPatternMatchesSameWireForm()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("agent", "loaded");

        await Assert.That(matcher.IsMatch("agent:loaded", type)).IsTrue();
    }

    [Test]
    public async Task ExactPatternDoesNotMatchDifferentEventName()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("agent", "unloaded");

        await Assert.That(matcher.IsMatch("agent:loaded", type)).IsFalse();
    }

    [Test]
    public async Task ExactPatternDoesNotMatchDifferentComponent()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("system", "loaded");

        await Assert.That(matcher.IsMatch("agent:loaded", type)).IsFalse();
    }

    [Test]
    public async Task ExactPatternDoesNotMatchTypeWithExtraSegment()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("agent", "loaded", "alpha");

        await Assert.That(matcher.IsMatch("agent:loaded", type)).IsFalse();
    }

    [Test]
    public async Task TrailingStarMatchesTwoSegmentType()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("agent", "loaded");

        await Assert.That(matcher.IsMatch("agent:*", type)).IsTrue();
    }

    [Test]
    public async Task TrailingStarMatchesThreeSegmentType()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("agent", "loaded", "alpha");

        await Assert.That(matcher.IsMatch("agent:*", type)).IsTrue();
    }

    [Test]
    public async Task TrailingStarDoesNotMatchDifferentComponent()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("system", "startup");

        await Assert.That(matcher.IsMatch("agent:*", type)).IsFalse();
    }

    [Test]
    public async Task TrailingPlusMatchesThreeSegmentType()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("agent", "loaded", "alpha");

        await Assert.That(matcher.IsMatch("agent:loaded:+", type)).IsTrue();
    }

    [Test]
    public async Task TrailingPlusDoesNotMatchTwoSegmentType()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("agent", "loaded");

        await Assert.That(matcher.IsMatch("agent:loaded:+", type)).IsFalse();
    }

    [Test]
    public async Task InteriorStarMatchesWithSegmentBetween()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("agent", "anything", "id");

        await Assert.That(matcher.IsMatch("agent:*:id", type)).IsTrue();
    }

    [Test]
    public async Task InteriorStarMatchesWithZeroSegmentsBetween()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("agent", "id");

        await Assert.That(matcher.IsMatch("agent:*:id", type)).IsTrue();
    }

    [Test]
    public async Task InteriorStarDoesNotMatchWhenTrailingSegmentDiffers()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("agent", "anything", "different");

        await Assert.That(matcher.IsMatch("agent:*:id", type)).IsFalse();
    }

    [Test]
    public async Task ThreeSegmentExactMatch()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("channel", "discord", "general");

        await Assert.That(matcher.IsMatch("channel:discord:general", type)).IsTrue();
    }

    [Test]
    public async Task ThreeSegmentTrailingStarMatchesAnyId()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("channel", "discord", "general");

        await Assert.That(matcher.IsMatch("channel:discord:*", type)).IsTrue();
    }

    [Test]
    public async Task ThreeSegmentTrailingStarDoesNotMatchDifferentMiddleSegment()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("channel", "slack", "general");

        await Assert.That(matcher.IsMatch("channel:discord:*", type)).IsFalse();
    }

    [Test]
    public async Task RepeatedCallWithSamePatternReturnsConsistentResult()
    {
        var matcher = new PatternMatcher();
        var match = new EventType("agent", "loaded");
        var miss = new EventType("agent", "unloaded");

        await Assert.That(matcher.IsMatch("agent:loaded", match)).IsTrue();
        await Assert.That(matcher.IsMatch("agent:loaded", miss)).IsFalse();
        await Assert.That(matcher.IsMatch("agent:loaded", match)).IsTrue();
    }

    [Test]
    public async Task LiteralIdSegmentWithDotMatches()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("channel", "discord", "general.lobby");

        await Assert.That(matcher.IsMatch("channel:discord:general.lobby", type)).IsTrue();
    }

    [Test]
    public async Task TrailingStarMatchesIdSegmentWithDot()
    {
        var matcher = new PatternMatcher();
        var type = new EventType("channel", "discord", "general.lobby");

        await Assert.That(matcher.IsMatch("channel:discord:*", type)).IsTrue();
    }
}
