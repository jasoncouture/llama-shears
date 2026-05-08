using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.Provider;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

internal static class TestAgentConfigs
{
    public static AgentConfig WithHeartbeat(TimeSpan heartbeatPeriod, string id = "test") =>
        new(
            Model: new AgentModelConfig(Id: new ModelIdentity("TEST", "stub")),
            ModelContextProtocolServers: [],
            Id: id)
        {
            HeartbeatPeriod = heartbeatPeriod,
        };

    public static AgentContext BuildAgentContext(string agentId) =>
        new(
            AgentId: agentId,
            Now: DateTimeOffset.UnixEpoch,
            Config: WithHeartbeat(TimeSpan.Zero),
            LanguageModel: new LanguageModelContext(Turns: [], Entries: [], ContextWindowTokenCount: 0),
            System: new SystemContext(),
            Tools: new ToolContext([]),
            Plugins: new PluginContext([]));

    // Default substitute returns null for IReadOnlyList<MemorySearchResult>;
    // Agent.SearchMemoriesAsync would NRE on hits.Count. Standard fixture
    // returns an empty list so any test that doesn't care about memory
    // enrichment behaves like an indexer-empty agent.
    public static IMemorySearcher EmptyMemorySearcher()
    {
        var searcher = Substitute.For<IMemorySearcher>();
        searcher
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<double?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyList<MemorySearchResult>>([]));
        return searcher;
    }
}
