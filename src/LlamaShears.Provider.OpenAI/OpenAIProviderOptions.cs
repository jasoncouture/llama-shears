using System.Text.Json.Nodes;

namespace LlamaShears.Provider.OpenAI;

public class OpenAiProviderOptions
{
    public Uri BaseUri { get; set; } = new Uri("http://localhost:8080");

    public string ApiKey { get; set; } = string.Empty;

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(300);

    public JsonObject ExtraRequestParams { get; set; } = [];

    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
