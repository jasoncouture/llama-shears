using System.Text;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Provider;

public sealed class ModelTextFormatter : IModelTextFormatter
{
    public string Format(ModelTurn turn)
    {
        ArgumentNullException.ThrowIfNull(turn);
        if (turn.Role != ModelRole.User)
        {
            return turn.Content;
        }
        var sb = new StringBuilder();
        sb.Append("[timestamp]");
        sb.Append(turn.Timestamp.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz"));
        sb.Append("[/timestamp]\n");
        if (!string.IsNullOrEmpty(turn.ChannelId))
        {
            sb.Append("[sourceChannel]");
            sb.Append(turn.ChannelId);
            sb.Append("[/sourceChannel]\n");
        }
        sb.Append(turn.Content);
        return sb.ToString();
    }
}
