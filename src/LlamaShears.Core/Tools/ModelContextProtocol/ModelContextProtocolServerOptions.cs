namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed class ModelContextProtocolServerOptions
{
    public Uri Uri { get; set; } = null!;
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
