namespace LlamaShears.Provider.Ollama;

public class OllamaProviderOptions
{
    public Uri BaseUri { get; set; } = new Uri("http://localhost:11434");

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(300);
}
