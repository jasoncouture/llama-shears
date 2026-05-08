using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

/// <summary>
/// Routes a model-issued <see cref="ToolCall"/> back to the server
/// that owns the tool, invokes it, and returns the textual result the
/// agent will hand back to the model on the next turn. Failures —
/// including cancellation, unknown tools, and tools outside the
/// caller-advertised set — are captured into the result rather than
/// thrown so the loop can keep going.
/// </summary>
public interface IToolCallDispatcher
{
    /// <summary>
    /// Validates <paramref name="call"/> against <paramref name="tools"/>
    /// (the set advertised to the model on this turn) and dispatches
    /// it. A call to a tool outside the advertised set comes back as
    /// an error result.
    /// </summary>
    ValueTask<ToolCallResult> DispatchAsync(
        ToolCall call,
        ImmutableArray<ToolGroup> tools,
        string eventId,
        Guid correlationId,
        CancellationToken cancellationToken);
}
