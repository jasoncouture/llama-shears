using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.Provider;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

internal static class TestAgentConfigs
{
    public static AgentConfig WithHeartbeat(TimeSpan heartbeatPeriod, string id = "test") =>
        new AgentConfig(Model: new ModelConfiguration(Id: new CompositeIdentity("TEST", "stub")),
            ModelContextProtocolServers: [], Id: id)
        {
            HeartbeatPeriod = heartbeatPeriod,
        };

    public static AgentContext BuildAgentContext(string agentId) =>
        new AgentContext(AgentId: agentId, Now: DateTimeOffset.UnixEpoch, Config: WithHeartbeat(TimeSpan.Zero),
            LanguageModel: new LanguageModelContext(Turns: [], Entries: [], ContextWindowTokenCount: 0),
            System: new SystemContext(), Tools: new ToolContext([]), Plugins: new PluginContext([]));

    public static IMemorySearcher EmptyMemorySearcher()
    {
        var searcher = Substitute.For<IMemorySearcher>();
        searcher
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<double?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyList<MemorySearchResult>>([]));
        return searcher;
    }

    public static IDataContextFactory DataContextFactoryWith(AgentConfig config)
    {
        var scope = Substitute.For<IDataContextScope>();
        scope.TryGetValue(AgentConfig.DataKey, out Arg.Any<AgentConfig?>())
            .Returns(call =>
            {
                call[1] = config;
                return true;
            });
        var factory = Substitute.For<IDataContextFactory>();
        factory.Current.Returns(scope);
        factory.TryJoinContextScope(Arg.Any<string>(), out Arg.Any<IDataContextScope?>())
            .Returns(call =>
            {
                call[1] = scope;
                return true;
            });
        return factory;
    }
}
