using OllamaSharp;

namespace LlamaShears.Provider.Ollama;

/// <summary>
/// Builds a fresh <see cref="IOllamaApiClient"/> configured for the
/// supplied <see cref="OllamaProviderOptions"/>. Each call returns a
/// new client backed by a new <see cref="HttpClient"/>; consumers own
/// the lifetime and should keep the client for as long as they intend
/// to hit the configured endpoint.
/// </summary>
public interface IOllamaApiClientFactory
{
    /// <summary>
    /// Returns a new <see cref="IOllamaApiClient"/> with
    /// <see cref="HttpClient.BaseAddress"/> and
    /// <see cref="HttpClient.Timeout"/> taken from
    /// <paramref name="options"/>.
    /// </summary>
    IOllamaApiClient CreateClient(OllamaProviderOptions options);
}
