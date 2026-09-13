using System.Text;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core;

public sealed class CompactionTranscriptFormatter
{
    public string Format(IReadOnlyList<ModelTurn> turns)
    {
        ArgumentNullException.ThrowIfNull(turns);

        var sb = new StringBuilder();
        foreach (var turn in turns)
        {
            sb.Append("<turn role=\"");
            sb.Append(RoleName(turn.Role));
            sb.Append('"');
            if (turn.IsError)
            {
                sb.Append(" error=\"true\"");
            }
            sb.Append(">\n");
            if (!string.IsNullOrEmpty(turn.Content))
            {
                sb.Append(turn.Content);
                sb.Append('\n');
            }
            if (!turn.ToolCalls.IsDefaultOrEmpty)
            {
                foreach (var call in turn.ToolCalls)
                {
                    sb.Append("<tool_call source=\"");
                    sb.Append(call.Source);
                    sb.Append("\" name=\"");
                    sb.Append(call.Name);
                    if (!string.IsNullOrEmpty(call.CallId))
                    {
                        sb.Append("\" call_id=\"");
                        sb.Append(call.CallId);
                    }
                    sb.Append("\">\n");
                    sb.Append(call.ArgumentsJson);
                    sb.Append("\n</tool_call>\n");
                }
            }
            sb.Append("</turn>\n");
        }
        return sb.ToString();
    }

    private static string RoleName(ModelRole role)
    {
        return role switch
        {
            ModelRole.User or ModelRole.FrameworkUser => "user",
            ModelRole.Assistant or ModelRole.FrameworkAssistant => "assistant",
            ModelRole.Tool => "tool",
            ModelRole.Thought => "thought",
            _ => role.ToString().ToLowerInvariant(),
        };
    }
}
