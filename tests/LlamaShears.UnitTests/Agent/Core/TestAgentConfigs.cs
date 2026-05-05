using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Provider;

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
}
