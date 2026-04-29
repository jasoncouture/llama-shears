using LlamaShears.Agent.Abstractions;
using LlamaShears.Agent.Core;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class InMemoryAgentTokenStoreTests
{
    [Test]
    public async Task Issue_returns_a_non_empty_base64_token_of_thirty_two_random_bytes()
    {
        var store = CreateStore(new FakeTimeProvider());

        var token = store.Issue(SampleAgent());
        var decoded = Convert.FromBase64String(token);

        await Assert.That(token).IsNotEmpty();
        await Assert.That(decoded.Length).IsEqualTo(32);
    }

    [Test]
    public async Task Issue_returns_distinct_tokens_for_repeated_calls()
    {
        var store = CreateStore(new FakeTimeProvider());

        var tokens = Enumerable.Range(0, 64)
            .Select(_ => store.Issue(SampleAgent()))
            .ToHashSet(StringComparer.Ordinal);

        await Assert.That(tokens.Count).IsEqualTo(64);
    }

    [Test]
    public async Task TryGetAgentInformation_returns_the_bound_agent_for_a_valid_token()
    {
        var store = CreateStore(new FakeTimeProvider());
        var expected = SampleAgent("alice");

        var token = store.Issue(expected);
        var ok = store.TryGetAgentInformation(token, out var actual);

        await Assert.That(ok).IsTrue();
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task TryGetAgentInformation_returns_false_for_an_unknown_token()
    {
        var store = CreateStore(new FakeTimeProvider());

        var ok = store.TryGetAgentInformation("not-a-real-token", out var actual);

        await Assert.That(ok).IsFalse();
        await Assert.That(actual).IsNull();
    }

    [Test]
    public async Task Token_can_only_be_consumed_once()
    {
        var store = CreateStore(new FakeTimeProvider());
        var token = store.Issue(SampleAgent());

        var first = store.TryGetAgentInformation(token, out _);
        var second = store.TryGetAgentInformation(token, out var afterConsume);

        await Assert.That(first).IsTrue();
        await Assert.That(second).IsFalse();
        await Assert.That(afterConsume).IsNull();
    }

    [Test]
    public async Task Token_expires_after_the_configured_lifetime()
    {
        var time = new FakeTimeProvider();
        var store = CreateStore(time, new AgentTokenStoreOptions { TokenLifetime = TimeSpan.FromSeconds(30) });
        var token = store.Issue(SampleAgent());

        time.Advance(TimeSpan.FromSeconds(31));
        var ok = store.TryGetAgentInformation(token, out var actual);

        await Assert.That(ok).IsFalse();
        await Assert.That(actual).IsNull();
    }

    [Test]
    public async Task Sweep_removes_only_expired_entries()
    {
        var time = new FakeTimeProvider();
        var store = CreateStore(time, new AgentTokenStoreOptions { TokenLifetime = TimeSpan.FromSeconds(30) });

        var earlyToken = store.Issue(SampleAgent("alice"));

        time.Advance(TimeSpan.FromSeconds(20));
        var lateToken = store.Issue(SampleAgent("bob"));

        time.Advance(TimeSpan.FromSeconds(15));

        var removed = store.Sweep();

        await Assert.That(removed).IsEqualTo(1);
        await Assert.That(store.TryGetAgentInformation(earlyToken, out _)).IsFalse();
        await Assert.That(store.TryGetAgentInformation(lateToken, out var bob)).IsTrue();
        await Assert.That(bob!.AgentId).IsEqualTo("bob");
    }

    private static InMemoryAgentTokenStore CreateStore(
        FakeTimeProvider time,
        AgentTokenStoreOptions? options = null)
        => new(time, Options.Create(options ?? new AgentTokenStoreOptions()));

    private static AgentInfo SampleAgent(string id = "alice")
        => new(id, "ollama:llama3", 8192);
}
