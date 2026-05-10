namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed class ModelContextProtocolOptions
{
    public Dictionary<string, Uri> Servers { get; set; } = new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase);
}
