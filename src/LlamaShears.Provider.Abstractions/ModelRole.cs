namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// Role of the participant that authored a <see cref="ModelTurn"/>.
/// <para>
/// <see cref="System"/>, <see cref="User"/>, and <see cref="Assistant"/>
/// correspond directly to the conventional chat-model roles. The
/// remaining values exist so the framework can tell the difference
/// between a turn that came from a real participant and a turn the
/// framework injected on its behalf:
/// </para>
/// <list type="bullet">
///   <item><see cref="FrameworkUser"/> — a "user" turn authored by the
///   framework rather than a human user (for example, scheduler
///   prompts, tool-result pseudo-messages, or framework-emitted
///   reminders). Provider implementations send these to the model
///   under the model's standard "user" role; the framework attaches a
///   prefix to the content to distinguish them for the model.</item>
///   <item><see cref="FrameworkAssistant"/> — an "assistant" turn that
///   responds to a <see cref="FrameworkUser"/> message. Sent under
///   the model's standard "assistant" role; the framework prefixes
///   the content as needed.</item>
/// </list>
/// </summary>
public enum ModelRole
{
    System,
    User,
    Assistant,
    FrameworkUser,
    FrameworkAssistant,
}
