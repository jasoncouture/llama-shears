namespace LlamaShears.Api.Tools.ModelContextProtocol;

internal static class ResponseBudget
{
    public const int MaxBytes = 32 * 1024;
    public const int MaxLines = 1000;

    public static bool CanAppendResponse(int bytesRead, int linesRead, string line)
    {
        if (linesRead >= MaxLines)
        {
            return false;
        }
        var estimated = (line?.Length ?? 0) + 1;
        return bytesRead + estimated <= MaxBytes;
    }
}
