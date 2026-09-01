using LlamaShears.Core.Abstractions.Agent.Pipeline;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Runs a single agent iteration from the pipeline bag: builds the
/// prompt (including <see cref="AgentPipelineContext.SystemPrompt"/>
/// and <see cref="AgentPipelineContext.EphemeralContext"/>),
/// invokes the language model (with the empty-response retry),
/// persists the model's output via the active context store, and
/// returns any tool-result turns the caller should feed back on the
/// next iteration. Knows nothing about session queues, agent locks,
/// or interrupt subscriptions — those concerns belong to the
/// surrounding onion.
/// </summary>
public interface IAgentIterationRunner
{
    /// <summary>
    /// Runs one iteration from <paramref name="context"/>. The caller
    /// is responsible for any lock acquisition, interrupt-token
    /// wiring, and acting on returned tool-result turns.
    /// </summary>
    /// <param name="context">
    /// The per-batch bag. Reads <see cref="AgentPipelineContext.AgentContext"/>,
    /// <see cref="AgentPipelineContext.Batch"/>,
    /// <see cref="AgentPipelineContext.CorrelationId"/>,
    /// <see cref="AgentPipelineContext.ShutdownToken"/>,
    /// <see cref="AgentPipelineContext.TurnToken"/>,
    /// <see cref="AgentPipelineContext.SystemPrompt"/>, and
    /// <see cref="AgentPipelineContext.EphemeralContext"/>.
    /// The run-iteration middleware stores the returned
    /// <see cref="IterationOutcome"/> on the bag.
    /// </param>
    Task<IterationOutcome> RunAsync(AgentPipelineContext context);
}
