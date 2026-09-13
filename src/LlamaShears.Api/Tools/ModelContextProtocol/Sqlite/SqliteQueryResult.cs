using System.Collections.Immutable;
using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Sqlite;

public sealed record SqliteQueryResult(
    string Path,
    int RowsAffected,
    ImmutableArray<string> Columns,
    ImmutableArray<ImmutableDictionary<string, object?>> Rows,
    int RowCount,
    bool Truncated,
    string? Error = null) : IToolResponse;
