using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Context;
using LlamaShears.UnitTests.Agent.Core;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Context;

public sealed class AgentContextProviderTests
{
    [Test]
    public async Task CreateAgentContextWithIdReturnsSnapshotForKnownAgent()
    {
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.FromMinutes(5));
        var configProvider = new StubAgentConfigProvider { Configs = { ["alpha"] = config } };
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 4, 30, 12, 0, 0, TimeSpan.Zero));
        var provider = new AgentContextProvider(configProvider, time);

        var context = provider.CreateAgentContext("alpha");

        await Assert.That(context).IsNotNull();
        await Assert.That(context!.AgentId).IsEqualTo("alpha");
        await Assert.That(context.Now).IsEqualTo(time.GetUtcNow());
        await Assert.That(context.Config).IsSameReferenceAs(config);
        await Assert.That(context.LanguageModel.Turns.Length).IsEqualTo(0);
        await Assert.That(context.LanguageModel.Entries.Length).IsEqualTo(0);
        await Assert.That(context.Tools.Items.Length).IsEqualTo(0);
        await Assert.That(context.Plugins.Data.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CreateAgentContextWithIdReturnsNullForUnknownAgent()
    {
        var configProvider = new StubAgentConfigProvider();
        var provider = new AgentContextProvider(configProvider, new FakeTimeProvider());

        var context = provider.CreateAgentContext("missing");

        await Assert.That(context).IsNull();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task CreateAgentContextWithBlankIdThrows(string? agentId)
    {
        var provider = new AgentContextProvider(new StubAgentConfigProvider(), new FakeTimeProvider());

        await Assert.That(() => provider.CreateAgentContext(agentId!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task ParameterlessCreateAgentContextThrowsNotImplemented()
    {
        var provider = new AgentContextProvider(new StubAgentConfigProvider(), new FakeTimeProvider());

        await Assert.That(() => provider.CreateAgentContext())
            .Throws<NotImplementedException>();
    }

    private sealed class StubAgentConfigProvider : IAgentConfigProvider
    {
        public Dictionary<string, AgentConfig> Configs { get; } = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> ListAgentIds() => [.. Configs.Keys];

        public AgentConfig? GetConfig(string agentId) =>
            Configs.TryGetValue(agentId, out var config) ? config : null;
    }
}
