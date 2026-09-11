namespace LlamaShears.Api.Tools.ModelContextProtocol.Http;

internal sealed record HttpBodyRead(
    string? Body,
    bool Truncated,
    bool Binary,
    string? ContentType,
    int? SavedBytes,
    bool SavedTruncated,
    string? SaveError);
