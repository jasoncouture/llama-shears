namespace LlamaShears.Provider.Ollama;

public class OllamaProviderOptions
{
    public Uri BaseUri { get; set; } = new("http://localhost:11434");

    // Time-to-first-byte cap on the chat HTTP call. Once Ollama starts
    // streaming this no longer applies (HttpCompletionOption.ResponseHeadersRead),
    // but cold-model loads on slow storage routinely run past the .NET
    // default of 100s before the first token. Setting this too low produces
    // a doom loop: the client times out, drops the TCP connection,
    // Ollama notices the disconnect and aborts the half-loaded runner,
    // so the next request starts cold again — and times out again.
    // 300s is the working ceiling for a 22 GB model on a spinning-disk
    // btrfs array; raise via appsettings if your storage is slower or
    // your models are larger.
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(300);
}
