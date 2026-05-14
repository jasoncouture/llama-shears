namespace LlamaShears.Provider.OpenAI;

internal static class OpenAiRequestHeaders
{
    public static void Apply(HttpRequestMessage request, IReadOnlyDictionary<string, string> headers)
    {
        foreach (var (name, value) in headers)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }
            request.Headers.Remove(name);
            request.Headers.TryAddWithoutValidation(name, value);
        }
    }
}
