namespace LlamaShears.Provider.Onnx.Embeddings;

public sealed class OnnxEmbeddingsProviderOptions
{
    public string? ModelsRoot { get; set; }

    public Dictionary<string, OnnxModelOptions> Models { get; set; } = new(StringComparer.Ordinal);

    public static string DefaultModelsRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".llama-shears",
        "models",
        "onnx");
}
