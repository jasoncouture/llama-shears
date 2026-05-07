using OllamaSharp;

namespace LlamaShears.Provider.Ollama;

/// <summary>
/// Builds an <see cref="IOllamaApiClient"/> configured for the supplied
/// <see cref="OllamaProviderOptions"/>. The underlying <see cref="HttpClient"/>
/// is acquired from <see cref="IHttpClientFactory"/>, so the handler and
/// connection pool are shared across calls.
/// </summary>
public interface IOllamaApiClientFactory
{
    /// <summary>
    /// Returns an <see cref="IOllamaApiClient"/> whose
    /// <see cref="HttpClient.BaseAddress"/> and
    /// <see cref="HttpClient.Timeout"/> reflect <paramref name="options"/>.
    /// </summary>
    IOllamaApiClient CreateClient(OllamaProviderOptions options);
}
