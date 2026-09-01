using System.Diagnostics;
using LlamaShears.Core.Abstractions;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Pipeline;

public sealed class AgentActivityMiddleware : IAgentMiddleware
{
    private static readonly ActivitySource _activitySource =
        Telemetry.CreateActivitySourceForType<AgentActivityMiddleware>();

    private readonly IDataContextScope _dataScope;

    public AgentActivityMiddleware(IDataContextScope dataScope)
    {
        _dataScope = dataScope;
    }

    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.AgentActivity;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity(
            name: $"chat {_dataScope.GetModelConfiguration().Id}",
            kind: ActivityKind.Client,
            tags: GetAgentTags());
        try
        {
            await next.Invoke(context, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }

    private IEnumerable<KeyValuePair<string, object?>> GetAgentTags()
    {
        var agentId = _dataScope.GetAgentConfig().Id;
        var sessionId = _dataScope.GetCurrentSessionId();
        var modelId = _dataScope.GetModelConfiguration().Id;
        var conversationId = sessionId.ToString();
        if (sessionId.IsDefault)
        {
            conversationId = $"{sessionId.AgentId}:{sessionId.Name}";
        }

        yield return new KeyValuePair<string, object?>("gen_ai.request.model", modelId.ToString());
        yield return new KeyValuePair<string, object?>("gen_ai.system", "llamashears");
        yield return new KeyValuePair<string, object?>("gen_ai.operation.name", "chat");
        yield return new KeyValuePair<string, object?>("gen_ai.agent.id", agentId);
        yield return new KeyValuePair<string, object?>("gen_ai.agent.name", agentId);
        yield return new KeyValuePair<string, object?>("gen_ai.agent.version", _activitySource.Version);
        yield return new KeyValuePair<string, object?>("gen_ai.conversation.id", conversationId);
    }
}
