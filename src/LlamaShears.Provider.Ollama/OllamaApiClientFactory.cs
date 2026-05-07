using OllamaSharp;

namespace LlamaShears.Provider.Ollama;

internal sealed class OllamaApiClientFactory : IOllamaApiClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OllamaApiClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IOllamaApiClient CreateClient(OllamaProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var http = _httpClientFactory.CreateClient(nameof(OllamaApiClientFactory));
        http.BaseAddress = options.BaseUri;
        http.Timeout = options.RequestTimeout;
        return new OllamaApiClient(http);
    }
}
