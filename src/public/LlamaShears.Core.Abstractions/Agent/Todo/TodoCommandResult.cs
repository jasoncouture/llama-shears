using System.Collections.Immutable;
using System.Text;

namespace LlamaShears.Core.Abstractions.Agent.Todo;

/// <summary>
/// Outcome of a todo-command invocation: the resulting items, an overall
/// state code, and an optional human-facing message that explains why the
/// command failed or why the list was rebuilt.
/// </summary>
/// <param name="Items">Todo items as they stand after the command applied.</param>
/// <param name="State">Overall outcome state — success, recovered-from-corruption, or refusal.</param>
/// <param name="Message">Free-form explanation surfaced alongside non-success states; <see langword="null"/> on plain success.</param>
public sealed record TodoCommandResult(ImmutableArray<TodoItem> Items, TodoResultState State, string? Message = null)
{
    private string? _toStringCache;
    /// <summary>Renders the result as the agent-facing string the tool returns: status prefix (when non-success) followed by the ordered todo list.</summary>
    public override string ToString() => _toStringCache ??= FormatResult();

    private string FormatResult()
    {
        var stringBuilder = new StringBuilder();
        var messagePrefix = State switch
        {
            TodoResultState.Success => "",
            TodoResultState.Corrupt => "WARNING: The todo list was corrupt, and a new empty list has been created",
            TodoResultState.Refused => "Refused",
            _ => throw new InvalidOperationException($"Unknown enum value {State:G} = {State}") // Intent - Show both string and numeric value.
        };

        if (!string.IsNullOrWhiteSpace(messagePrefix))
        {
            stringBuilder.Append(messagePrefix);
            if (!string.IsNullOrWhiteSpace(Message))
            {
                stringBuilder.Append(": ").Append(Message);
            }
            stringBuilder.Append("\n\n");
        }

        foreach (var item in Items.OrderBy(i => i.Index))
        {
            // \n is used here so that it's consistent across platforms.
            // Will read line handle this correctly? :grimace:
            stringBuilder.Append($"{item}\n");
        }

        return stringBuilder.ToString().TrimEnd('\n');
    }
}