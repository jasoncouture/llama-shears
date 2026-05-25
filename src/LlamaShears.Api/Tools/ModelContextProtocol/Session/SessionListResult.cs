using System.Collections.Immutable;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Session;

public sealed record SessionListResult(int SessionCount, ImmutableArray<SessionEntry> Sessions, string? Error = null);
