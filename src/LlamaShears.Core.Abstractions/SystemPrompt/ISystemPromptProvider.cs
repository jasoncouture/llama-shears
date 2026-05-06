namespace LlamaShears.Core.Abstractions.SystemPrompt;

/// <summary>
/// Builds the system-prompt body that the agent prepends to every
/// prompt cycle. Implementations decide how the body is assembled
/// (hard-coded, template-driven, plugin-contributed) and where any
/// time-dependent content comes from.
/// </summary>
public interface ISystemPromptProvider
{
    /// <summary>
    /// Builds the system-prompt body for <paramref name="agentId"/>.
    /// The string returned is fed to the model as the prompt's
    /// <see cref="LlamaShears.Core.Abstractions.Provider.ModelRole.System"/>
    /// turn.
    /// </summary>
    string Build(string agentId);
}
