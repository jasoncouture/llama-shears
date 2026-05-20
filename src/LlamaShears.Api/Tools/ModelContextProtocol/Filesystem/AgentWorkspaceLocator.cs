using System.Security.Claims;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
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
        var claim = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(claim) || !SessionId.TryParse(claim, out var session))
        {
            return new AgentWorkspace(null, Environment.CurrentDirectory);
        }
        var config = await _configs.GetConfigAsync(session.AgentId, cancellationToken);
        var root = config is null || string.IsNullOrEmpty(config.WorkspacePath)
            ? Environment.CurrentDirectory
            : Path.GetFullPath(config.WorkspacePath);
        return new AgentWorkspace(session.AgentId, root);
    }
}
