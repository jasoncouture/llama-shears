using LlamaShears.Core.Abstractions.Agent.Pipeline;

namespace LlamaShears.UnitTests.Agent.Pipeline;

internal sealed class RecordingAgentMiddleware : IAgentMiddleware
{
    private readonly string _name;
    private readonly IList<string> _trace;
    private readonly bool _callNext;

    public RecordingAgentMiddleware(IList<string> trace)
        : this("step", trace, callNext: true)
    {
    }

    public RecordingAgentMiddleware(string name, IList<string> trace, bool callNext = true)
    {
        _name = name;
        _trace = trace;
        _callNext = callNext;
    }

    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        _trace.Add($"{_name}-before");
        if (_callNext)
        {
            await next.Invoke(context, cancellationToken);
        }

        _trace.Add($"{_name}-after");
    }
}
