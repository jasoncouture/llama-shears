namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed class ModelContextProtocolOptions
{
    public Dictionary<string, ModelContextProtocolServerOptions> Servers { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);
}
