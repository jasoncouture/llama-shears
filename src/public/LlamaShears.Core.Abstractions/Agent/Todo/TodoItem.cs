namespace LlamaShears.Core.Abstractions.Agent.Todo;

/// <summary>
/// Single entry in an agent's persistent todo list.
/// </summary>
/// <param name="Index">1-based ordinal position used both for display and for addressing the item in updates.</param>
/// <param name="Text">Free-form todo text written by the agent.</param>
/// <param name="Completed"><see langword="true"/> when the agent has marked the item done.</param>
public sealed record TodoItem(int Index, string Text, bool Completed)
{
    private string? _toStringCache;
    /// <summary>Renders the item in checkbox form (<c>1. [x] text</c>) for the agent-facing tool transcript.</summary>
    public override string ToString() => _toStringCache ??= $"{Index}. [{(Completed ? 'x' : ' ')}] {Text}";
}