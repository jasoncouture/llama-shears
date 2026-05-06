namespace LlamaShears.Provider.Ollama;

internal static class OllamaKeepAlive
{
    // Ollama wants whole minutes with the "m" suffix for inactivity
    // timeouts; "0" forces immediate unload, "-1" pins indefinitely.
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
