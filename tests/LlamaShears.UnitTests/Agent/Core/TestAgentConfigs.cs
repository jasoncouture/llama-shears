using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Provider;

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
