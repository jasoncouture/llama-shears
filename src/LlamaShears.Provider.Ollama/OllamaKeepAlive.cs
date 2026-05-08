namespace LlamaShears.Provider.Ollama;

internal static class OllamaKeepAlive
{
    public static string? Map(TimeSpan? keepAlive)
    {
        if (keepAlive is not { } span)
        {
            return null;
        }

        if (span == TimeSpan.Zero)
        {
            return "0";
        }

        if (span < TimeSpan.Zero)
        {
            return "-1";
        }

        return $"{(long)Math.Round(span.TotalMinutes)}m";
    }
}
