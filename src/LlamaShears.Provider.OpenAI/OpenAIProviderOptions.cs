using System.Text.Json.Nodes;

namespace LlamaShears.Provider.OpenAI;

public class OpenAIProviderOptions
{
    public Uri BaseUri { get; set; } = new("http://localhost:8080");

    public string ApiKey { get; set; } = string.Empty;

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(300);

    public JsonObject ExtraRequestParams { get; set; } = [];
}
