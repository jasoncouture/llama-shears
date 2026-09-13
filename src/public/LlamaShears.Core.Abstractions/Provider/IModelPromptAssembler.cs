using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Prepends the framework system prompt and ephemeral context onto
/// caller-supplied turns. Template names come from the in-scope
/// <see cref="AgentConfig"/>; callers overlay that config when they
/// need a different system or prompt-context template.
/// </summary>
public interface IModelPromptAssembler
{
    /// <summary>
    /// Returns a prompt of
    /// <c>[System, SystemEphemeral?, ..<paramref name="turns"/>]</c>.
    /// The ephemeral turn is omitted when the prompt-context template
    /// renders to whitespace. When present, it is inserted immediately
    /// before the trailing user cluster (the same rule the agent
    /// pipeline uses).
    /// </summary>
    ValueTask<ModelPrompt> AssembleAsync(
        IReadOnlyList<ModelTurn> turns,
        CancellationToken cancellationToken);
}
