using LlamaShears.Agent.Core;
using LlamaShears.Api.Web.Services;

namespace LlamaShears.Api.Web;

internal sealed class AgentDirectory : IAgentDirectory
{
    private readonly AgentManager _manager;

    public AgentDirectory(AgentManager manager)
    {
        _manager = manager;
    }

    public IReadOnlyList<string> ListAgentIds()
        => [.. _manager.Agents.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)];
}
