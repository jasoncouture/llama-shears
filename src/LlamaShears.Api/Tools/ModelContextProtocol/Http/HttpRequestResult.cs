using System.Collections.Immutable;
using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Http;

public sealed record HttpRequestResult(
    bool Completed,
    int? Status,
    string? ReasonPhrase,
    bool Ok,
    string? Url,
    ImmutableDictionary<string, string> Headers,
    string? ContentType,
    string? Body,
    bool Truncated,
    bool Binary,
    bool TimedOut,
    int ElapsedMilliseconds,
    string? SavedPath = null,
    int? SavedBytes = null,
    bool SavedTruncated = false,
    string? Error = null) : IToolResponse;
