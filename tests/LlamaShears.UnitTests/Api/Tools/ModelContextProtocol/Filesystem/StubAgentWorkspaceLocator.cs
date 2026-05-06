using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;

internal sealed class StubAgentWorkspaceLocator : IAgentWorkspaceLocator
{
    private readonly AgentWorkspace _workspace;

    public StubAgentWorkspaceLocator(AgentWorkspace workspace)
    {
        _workspace = workspace;
    }

    public Task<AgentWorkspace> GetAsync(CancellationToken cancellationToken) => Task.FromResult(_workspace);
}
