using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent.Pipeline;

/// <summary>
/// Mutable bag handed to every <see cref="IAgentMiddleware"/> for one
/// dequeued batch. The loop owner constructs it; middleware fill
/// <see cref="CorrelationId"/>, <see cref="TurnToken"/>,
/// <see cref="SystemPrompt"/>, <see cref="EphemeralContext"/>,
/// <see cref="Prompt"/>, and <see cref="Outcome"/> as they run.
/// </summary>
public sealed class AgentPipelineContext
{
    /// <summary>
    /// Builds a context for <paramref name="batch"/>.
    /// <see cref="TurnToken"/> starts as <paramref name="shutdownToken"/>
    /// so steps that run before interrupt-scope wiring still have a token.
    /// </summary>
    /// <param name="agentContext">Live persisted conversation for the session being driven.</param>
    /// <param name="batch">Inbound turns dequeued for this iteration (user and/or tool).</param>
    /// <param name="shutdownToken">Loop-level cancellation; also the initial <see cref="TurnToken"/>.</param>
    public AgentPipelineContext(
        IAgentContext agentContext,
        ImmutableArray<ModelTurn> batch,
        CancellationToken shutdownToken)
    {
        ArgumentNullException.ThrowIfNull(agentContext);
        AgentContext = agentContext;
        Batch = batch;
        ShutdownToken = shutdownToken;
        TurnToken = shutdownToken;
    }

    /// <summary>
    /// Live persisted conversation for the session. Token metrics and
    /// any turn the iteration persists land here.
    /// </summary>
    public IAgentContext AgentContext { get; }

    /// <summary>
    /// Inbound turns for this iteration. Middleware may replace the
    /// array (filter, coalesce) before the iteration runner sees it.
    /// </summary>
    public ImmutableArray<ModelTurn> Batch { get; set; }

    /// <summary>
    /// Correlation id stamped on events published during the
    /// iteration. Empty until correlation-scope middleware assigns one.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Loop-level cancellation. Outlives a turn interrupt — tail
    /// persistence that must finish after interrupt uses this, not
    /// <see cref="TurnToken"/>.
    /// </summary>
    public CancellationToken ShutdownToken { get; }

    /// <summary>
    /// Cancellation linked to interrupt signals. Defaults to
    /// <see cref="ShutdownToken"/> until interrupt-scope middleware
    /// installs a linked source.
    /// </summary>
    public CancellationToken TurnToken { get; set; }

    /// <summary>
    /// Persistent system-prompt turn for this batch. <see langword="null"/>
    /// until system-prompt middleware renders it. Not persisted; compaction
    /// prepends it when building <see cref="Prompt"/>.
    /// </summary>
    public ModelTurn? SystemPrompt { get; set; }

    /// <summary>
    /// Per-turn prompt-context turn for this batch. <see langword="null"/>
    /// until ephemeral-context middleware renders it, or when the
    /// template is empty. Not persisted; the iteration inserts it
    /// once, immediately before the last user cluster when
    /// <see cref="Prompt"/> ends in a user turn.
    /// </summary>
    public ModelTurn? EphemeralContext { get; set; }

    /// <summary>
    /// Compacted model prompt for this batch. <see langword="null"/>
    /// until compaction middleware builds it (and possibly rewrites
    /// it). The iteration sends this, after inserting
    /// <see cref="EphemeralContext"/>.
    /// </summary>
    public ModelPrompt? Prompt { get; set; }

    /// <summary>
    /// Result of the iteration runner. <see langword="null"/> until
    /// the run-iteration step completes (or the chain short-circuits).
    /// </summary>
    public IterationOutcome? Outcome { get; set; }
}
