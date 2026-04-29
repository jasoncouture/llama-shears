namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// Role of the participant that authored a <see cref="ModelTurn"/>.
/// </summary>
public enum ModelRole
{
    System,
    User,
    Assistant
}
