namespace LlamaShears.Provider.Ollama;

/// <summary>
/// Configuration for the Ollama provider.
/// </summary>
public class OllamaProviderOptions
{
    /// <summary>
    /// Base URI of the Ollama server. Defaults to <c>http://localhost:11434</c>.
    /// </summary>
    public Uri BaseUri { get; set; } = new("http://localhost:11434");
}
