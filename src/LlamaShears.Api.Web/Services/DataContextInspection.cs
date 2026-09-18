using System.Collections.Immutable;

namespace LlamaShears.Api.Web.Services;

public sealed record DataContextInspection(
    string SessionId,
    ImmutableArray<DataContextEntry> Entries,
    string? Error)
{
    public static DataContextInspection Empty(string sessionId)
        => new DataContextInspection(sessionId, [], null);
}
