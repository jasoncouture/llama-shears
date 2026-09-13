using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;

namespace LlamaShears.Core.Pipeline;

public sealed class SystemPromptMiddleware : IAgentMiddleware
{
    private readonly ISystemPromptProvider _systemPrompt;
    private readonly IDataContextScope _dataScope;
    private readonly TimeProvider _time;

    public SystemPromptMiddleware(
        ISystemPromptProvider systemPrompt,
        IDataContextScope dataScope,
        TimeProvider time)
    {
        _systemPrompt = systemPrompt;
        _dataScope = dataScope;
        _time = time;
    }

    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.SystemPrompt;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        var template = _dataScope.GetAgentConfig().SystemPrompt;
        var body = await _systemPrompt.GetAsync(template, _dataScope.Snapshot(), context.TurnToken);
        context.SystemPrompt = new ModelTurn(ModelRole.System, body, _time.GetLocalNow());
        if (context.Prompt is { } prompt)
        {
            context.Prompt = PrependSystem(prompt, context.SystemPrompt);
        }
        await next.Invoke(context, cancellationToken);
    }

    private static ModelPrompt PrependSystem(ModelPrompt prompt, ModelTurn system)
    {
        if (prompt.Turns.Count > 0 && prompt.Turns[0].Role == ModelRole.System)
        {
            return new ModelPrompt([system, .. prompt.Turns.Skip(1)]);
        }

        return new ModelPrompt([system, .. prompt.Turns]);
    }
}
