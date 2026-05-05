using OllamaSharp;

namespace LlamaShears.Provider.Ollama;

internal sealed class OllamaApiClientFactory : IOllamaApiClientFactory
{
    public IOllamaApiClient CreateClient(OllamaProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var http = new HttpClient
        {
            BaseAddress = options.BaseUri,
            Timeout = options.RequestTimeout,
        };
        return new OllamaApiClient(http);
    }
}
