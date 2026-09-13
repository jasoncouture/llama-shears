using System.Collections.Immutable;
using System.ComponentModel;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Sqlite;

[McpServerToolType]
public sealed partial class SqliteTools
{
    public const int DefaultMaxRows = 100;
    public const int HardMaxRows = 1000;
    public const int BusyTimeoutMilliseconds = 5000;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IPathExpander _pathExpander;
    private readonly IFileProtectionPolicy _protection;
    private readonly ILogger<SqliteTools> _logger;

    public SqliteTools(
        IAgentWorkspaceLocator workspace,
        IPathExpander pathExpander,
        IFileProtectionPolicy protection,
        ILogger<SqliteTools> logger)
    {
        _workspace = workspace;
        _pathExpander = pathExpander;
        _protection = protection;
        _logger = logger;
    }

    [McpServerTool(Name = "sqlite_query", OpenWorld = false)]
    [Description("Executes SQL against a SQLite database file inside the agent's workspace. Use this instead of shell_run + sqlite3. path is workspace-relative (parent directories are created if missing). Writes into system/ or outside the workspace are refused. sql may be multiple semicolon-delimited statements (DDL, DML, SELECT, PRAGMA). When several statements return rows, the last result set is returned and rowsAffected is aggregated. maxRows defaults to 100 and is hard-capped at 1000; truncated=true when more rows remain. INTEGER/REAL/TEXT/NULL map to JSON number/number/string/null; BLOB is Base64. SQL errors populate error and do not fail the tool call.")]
    public async Task<SqliteQueryResult> Query(
        [Description("Workspace-relative path to the SQLite database file (for example state/metrics.db). Absolute paths must still resolve inside the workspace.")]
        string path,
        [Description("SQL to execute. Multiple semicolon-delimited statements are allowed.")]
        string sql,
        [Description("Maximum rows to return from the last result set. Defaults to 100; hard-capped at 1000.")]
        int maxRows = DefaultMaxRows,
        CancellationToken cancellationToken = default)
    {
        path ??= string.Empty;
        if (string.IsNullOrWhiteSpace(sql))
        {
            return Failure(path, "sql is required.");
        }

        var cap = Math.Clamp(maxRows, 1, HardMaxRows);
        var workspace = await _workspace.GetAsync(cancellationToken);
        var resolution = WorkspacePathResolver.ResolveForWrite(workspace, path);
        if (!resolution.IsSuccess)
        {
            return Failure(path, resolution.Error);
        }

        if (Directory.Exists(resolution.FullPath))
        {
            return Failure(path, $"Refused: '{path}' is an existing directory.");
        }

        var expanded = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, expanded, FileType.File, ProtectionMode.Write);
        if (protection is not null)
        {
            return Failure(path, ProtectionRefusal.Format(path, ProtectionMode.Write, protection));
        }

        var statements = SqliteBatchSplitter.Split(sql);
        if (statements.Length == 0)
        {
            return Failure(path, "sql is required.");
        }

        try
        {
            var parent = Path.GetDirectoryName(resolution.FullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = resolution.FullPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
            }.ToString();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await ApplyPragmasAsync(connection, cancellationToken);

            var rowsAffected = 0;
            ImmutableArray<string> columns = [];
            ImmutableArray<ImmutableDictionary<string, object?>> rows = [];
            var truncated = false;

            foreach (var statement in statements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var command = connection.CreateCommand();
                command.CommandText = statement;
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (reader.RecordsAffected > 0)
                {
                    rowsAffected += reader.RecordsAffected;
                }

                if (reader.FieldCount == 0)
                {
                    continue;
                }

                var captured = ReadResult(reader, cap);
                columns = captured.Columns;
                rows = captured.Rows;
                truncated = captured.Truncated;
            }

            LogQuery(workspace.AgentId, resolution.FullPath, rowsAffected, rows.Length, truncated);
            return new SqliteQueryResult(
                Path: path,
                RowsAffected: rowsAffected,
                Columns: columns,
                Rows: rows,
                RowCount: rows.Length,
                Truncated: truncated);
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            LogQueryFailed(workspace.AgentId, resolution.FullPath, ex.Message, ex);
            return Failure(path, ex.Message);
        }
    }

    private static async Task ApplyPragmasAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using (var journal = connection.CreateCommand())
        {
            journal.CommandText = "PRAGMA journal_mode = WAL;";
            await journal.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var busy = connection.CreateCommand())
        {
            busy.CommandText = $"PRAGMA busy_timeout = {BusyTimeoutMilliseconds};";
            await busy.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static (ImmutableArray<string> Columns, ImmutableArray<ImmutableDictionary<string, object?>> Rows, bool Truncated) ReadResult(
        SqliteDataReader reader,
        int maxRows)
    {
        var columns = new string[reader.FieldCount];
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            columns[i] = string.IsNullOrEmpty(name) ? $"column{i}" : name;
        }

        var rows = new List<ImmutableDictionary<string, object?>>(Math.Min(maxRows, 32));
        var truncated = false;
        while (reader.Read())
        {
            if (rows.Count == maxRows)
            {
                truncated = true;
                break;
            }

            var builder = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.Ordinal);
            for (var i = 0; i < columns.Length; i++)
            {
                builder[columns[i]] = ReadValue(reader, i);
            }

            rows.Add(builder.ToImmutable());
        }

        return ([.. columns], [.. rows], truncated);
    }

    private static object? ReadValue(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var fieldType = reader.GetFieldType(ordinal);
        if (fieldType == typeof(long) || fieldType == typeof(int) || fieldType == typeof(short) || fieldType == typeof(byte))
        {
            return reader.GetInt64(ordinal);
        }

        if (fieldType == typeof(double) || fieldType == typeof(float) || fieldType == typeof(decimal))
        {
            return reader.GetDouble(ordinal);
        }

        if (fieldType == typeof(string))
        {
            return reader.GetString(ordinal);
        }

        if (fieldType == typeof(byte[]))
        {
            return Convert.ToBase64String((byte[])reader.GetValue(ordinal));
        }

        if (fieldType == typeof(bool))
        {
            return reader.GetBoolean(ordinal);
        }

        if (fieldType == typeof(DateTime))
        {
            var value = reader.GetDateTime(ordinal);
            var utc = value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
            return new DateTimeOffset(utc);
        }

        return reader.GetValue(ordinal) switch
        {
            byte[] blob => Convert.ToBase64String(blob),
            DateTime dateTime => new DateTimeOffset(
                dateTime.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                    : dateTime.ToUniversalTime()),
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DBNull => null,
            var value => value,
        };
    }

    private static SqliteQueryResult Failure(string path, string error) =>
        new(path, 0, [], [], 0, false, error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' executed sqlite_query on '{Path}' (rowsAffected={RowsAffected}, rowCount={RowCount}, truncated={Truncated}).")]
    private partial void LogQuery(string? agentId, string path, int rowsAffected, int rowCount, bool truncated);

    [LoggerMessage(Level = LogLevel.Warning, Message = "sqlite_query failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogQueryFailed(string? agentId, string path, string message, Exception ex);
}
