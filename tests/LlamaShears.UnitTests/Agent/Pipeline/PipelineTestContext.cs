using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.UnitTests.Agent.Core;

namespace LlamaShears.UnitTests.Agent.Pipeline;

internal static class PipelineTestContext
{
    public static AgentPipelineContext Create(CancellationToken shutdownToken = default)
    {
        return new AgentPipelineContext(
            new FakeAgentContext("alice"),
            [],
            shutdownToken);
    }

    public static IDataContextScope ScopeFor(string agentId = "alice")
    {
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, agentId);
        var session = new SessionId(agentId, SessionId.DefaultSessionName);
        IDataContextScope scope = new FakeDataContextScope(session);
        scope.SetItem(AgentConfig.DataKey, config);
        scope.SetItem(LlamaShears.Core.Abstractions.Provider.ModelConfiguration.DataKey, config.Model);
        scope.SetItem(SessionPath.DataKey, new SessionPath(session));
        return scope;
    }
}
