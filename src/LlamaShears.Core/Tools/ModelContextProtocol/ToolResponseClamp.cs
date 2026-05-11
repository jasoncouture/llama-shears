using System.IO;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

internal static class ToolResponseClamp
{
    private const int MaxLines = 150;
    private const int MaxChars = 8192 + 1024;
    private const string OverflowMarker = "[Output limits exceeded; the rest of the response was dropped. Re-run with a more targeted command or query.]";

    public static string Apply(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return raw ?? string.Empty;
        }

        using var reader = new StringReader(raw);
        using var writer = new StringWriter { NewLine = "\n" };
        var lines = 0;
        var chars = 0;
        var truncated = false;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (lines >= MaxLines || chars + line.Length + 1 > MaxChars)
            {
                truncated = true;
                break;
            }
            writer.WriteLine(line);
            lines++;
            chars += line.Length + 1;
        }
        if (truncated)
        {
            writer.WriteLine(OverflowMarker);
        }
        return writer.ToString();
    }
}
