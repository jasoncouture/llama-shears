namespace LlamaShears.Provider.Onnx.Embeddings;

public sealed class OnnxModelOptions
{
    public string ModelPath { get; set; } = string.Empty;
    public string VocabPath { get; set; } = string.Empty;
    public int MaxSequenceLength { get; set; } = 256;
    public bool LowerCase { get; set; } = true;
    public OnnxPoolingStrategy Pooling { get; set; } = OnnxPoolingStrategy.Mean;
    public bool Normalize { get; set; } = true;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
}
