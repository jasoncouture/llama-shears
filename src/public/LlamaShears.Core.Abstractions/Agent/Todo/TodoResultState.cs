namespace LlamaShears.Core.Abstractions.Agent.Todo;

/// <summary>Overall outcome reported by a todo command.</summary>
public enum TodoResultState
{
    /// <summary>Command applied normally and the returned items reflect the persisted state.</summary>
    Success,
    /// <summary>Underlying todo store was unreadable / malformed and a fresh empty list replaced it before the command applied.</summary>
    Corrupt,
    /// <summary>Command was rejected (e.g. validation failure) and the persisted state is unchanged.</summary>
    Refused
}
