namespace LlamaShears.Data.Entities;

/// <summary>
/// Role of a participant in an LLM context exchange. Mirrors
/// <c>LlamaShears.Provider.Abstractions.ModelRole</c> for the values
/// that originate from the provider, plus <see cref="Tool"/> for
/// tool-result entries that are persisted but not represented as a
/// distinct provider role.
/// <para>
/// <see cref="System"/>, <see cref="User"/>, and <see cref="Assistant"/>
/// correspond directly to the conventional chat-model roles.
/// <see cref="FrameworkUser"/> and <see cref="FrameworkAssistant"/>
/// distinguish framework-injected turns from real participant turns;
/// see <c>ModelRole</c> for the full rationale.
/// </para>
/// </summary>
public enum SessionMessageRole
{
    System,
    User,
    Assistant,
    FrameworkUser,
    FrameworkAssistant,
    Tool,
}
