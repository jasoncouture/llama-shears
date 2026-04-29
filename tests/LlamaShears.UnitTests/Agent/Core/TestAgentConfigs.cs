using LlamaShears.Agent.Core;
using LlamaShears.Provider.Abstractions;

namespace LlamaShears.UnitTests.Agent.Core;

internal static class TestAgentConfigs
{
    public static AgentConfig WithHeartbeat(TimeSpan heartbeatPeriod) =>
        new()
        {
            Model = new AgentModelConfig
            {
                Id = new ModelIdentity("TEST", "stub"),
            },
            HeartbeatPeriod = heartbeatPeriod,
        };
}
