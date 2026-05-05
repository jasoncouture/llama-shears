using System.Security.Claims;
using LlamaShears.Core.Abstractions.Agent;
using Microsoft.AspNetCore.Http;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed class AgentWorkspaceLocator : IAgentWorkspaceLocator
{
    private readonly IHttpContextAccessor _http;
    private readonly IAgentConfigProvider _configs;

    public AgentWorkspaceLocator(IHttpContextAccessor http, IAgentConfigProvider configs)
    {
        _http = http;
        _configs = configs;
    }

    public async Task<AgentWorkspace> GetAsync(CancellationToken cancellationToken)
    {
        var agentId = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(agentId))
        {
            return new AgentWorkspace(null, Environment.CurrentDirectory);
        }
        var config = await _configs.GetConfigAsync(agentId, cancellationToken).ConfigureAwait(false);
        var root = config is null || string.IsNullOrEmpty(config.WorkspacePath)
            ? Environment.CurrentDirectory
            : Path.GetFullPath(config.WorkspacePath);
        return new AgentWorkspace(agentId, root);
    }
}
