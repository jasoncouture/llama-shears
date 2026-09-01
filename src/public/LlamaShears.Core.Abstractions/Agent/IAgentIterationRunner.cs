using LlamaShears.Core.Abstractions.Agent.Pipeline;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Runs a single agent iteration from the pipeline bag: takes
/// <see cref="AgentPipelineContext.Prompt"/> (compaction plus
/// ephemeral insert already applied), invokes the language model
/// (with the empty-response retry), persists token metrics, and
/// returns the model's tool calls on <see cref="IterationOutcome"/>.
/// Dispatch middleware executes those calls. Knows nothing about
/// session queues, agent locks, or interrupt subscriptions.
/// </summary>
public interface IAgentIterationRunner
{
    /// <summary>
    /// Runs one iteration from <paramref name="context"/>. The caller
    /// is responsible for any lock acquisition and interrupt-token
    /// wiring. Dispatch middleware acts on returned tool calls.
    /// </summary>
    /// <param name="context">
    /// The per-batch bag. Reads <see cref="AgentPipelineContext.AgentContext"/>,
    /// <see cref="AgentPipelineContext.Batch"/>,
    /// <see cref="AgentPipelineContext.CorrelationId"/>,
    /// <see cref="AgentPipelineContext.TurnToken"/>,
    /// <see cref="AgentPipelineContext.Prompt"/>,
    /// <see cref="AgentPipelineContext.SessionId"/>, and
    /// <see cref="AgentPipelineContext.ChannelId"/>.
    /// The run-iteration middleware stores the returned
    /// <see cref="IterationOutcome"/> on the bag.
    /// </param>
    Task<IterationOutcome> RunAsync(AgentPipelineContext context);
}
