using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Api.Tools.ModelContextProtocol.Sqlite;
using LlamaShears.Core.Paths;
using LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Sqlite;

public sealed class SqliteToolsTests
{
    [Test]
    public async Task CreatesDatabaseFileIfMissing()
    {
        using var temp = TempWorkspace.Create();
        var tool = CreateTool(temp);

        try
        {
            var result = await tool.Query("state/metrics.db", "SELECT 1 AS ok;", cancellationToken: CancellationToken.None);

            await Assert.That(result.Error).IsNull();
            await Assert.That(result.Path).IsEqualTo("state/metrics.db");
            await Assert.That(File.Exists(temp.PathOf("state", "metrics.db"))).IsTrue();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
        }
    }

    [Test]
    public async Task ExecutesDdlAndInsert()
    {
        using var temp = TempWorkspace.Create();
        var tool = CreateTool(temp);

        try
        {
            var result = await tool.Query(
                "app.db",
                "CREATE TABLE items(id INTEGER PRIMARY KEY, name TEXT); INSERT INTO items(name) VALUES ('a'); INSERT INTO items(name) VALUES ('b');",
                cancellationToken: CancellationToken.None);

            await Assert.That(result.Error).IsNull();
            await Assert.That(result.RowsAffected).IsEqualTo(2);
            await Assert.That(result.RowCount).IsEqualTo(0);
            await Assert.That(result.Columns).IsEmpty();
            await Assert.That(result.Truncated).IsFalse();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
        }
    }

    [Test]
    public async Task SelectsRowsWithJsonTypes()
    {
        using var temp = TempWorkspace.Create();
        var tool = CreateTool(temp);

        try
        {
            var result = await tool.Query(
                "types.db",
                """
                CREATE TABLE sample(id INTEGER, score REAL, name TEXT, note TEXT, blob BLOB);
                INSERT INTO sample(id, score, name, note, blob) VALUES (1, 2.5, 'alpha', NULL, X'0102');
                SELECT id, score, name, note, blob FROM sample;
                """,
                cancellationToken: CancellationToken.None);

            await Assert.That(result.Error).IsNull();
            await Assert.That(result.RowCount).IsEqualTo(1);
            await Assert.That(result.Columns).IsEquivalentTo(["id", "score", "name", "note", "blob"]);
            var row = result.Rows[0];
            await Assert.That(row["id"]).IsEqualTo(1L);
            await Assert.That(row["score"]).IsEqualTo(2.5);
            await Assert.That(row["name"]).IsEqualTo("alpha");
            await Assert.That(row["note"]).IsNull();
            await Assert.That(row["blob"]).IsEqualTo(Convert.ToBase64String([0x01, 0x02]));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
        }
    }

    [Test]
    public async Task EnforcesMaxRowsTruncation()
    {
        using var temp = TempWorkspace.Create();
        var tool = CreateTool(temp);
        var values = string.Join(",", Enumerable.Range(1, 200).Select(i => $"({i})"));

        try
        {
            var result = await tool.Query(
                "rows.db",
                $"CREATE TABLE nums(id INTEGER); INSERT INTO nums(id) VALUES {values}; SELECT id FROM nums ORDER BY id;",
                maxRows: 100,
                cancellationToken: CancellationToken.None);

            await Assert.That(result.Error).IsNull();
            await Assert.That(result.RowCount).IsEqualTo(100);
            await Assert.That(result.Rows.Length).IsEqualTo(100);
            await Assert.That(result.Truncated).IsTrue();
            await Assert.That(result.RowsAffected).IsEqualTo(200);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
        }
    }

    [Test]
    public async Task CapsMaxRowsAtHardCeiling()
    {
        using var temp = TempWorkspace.Create();
        var tool = CreateTool(temp);
        var values = string.Join(",", Enumerable.Range(1, 1001).Select(i => $"({i})"));

        try
        {
            var result = await tool.Query(
                "cap.db",
                $"CREATE TABLE nums(id INTEGER); INSERT INTO nums(id) VALUES {values}; SELECT id FROM nums ORDER BY id;",
                maxRows: 5000,
                cancellationToken: CancellationToken.None);

            await Assert.That(result.Error).IsNull();
            await Assert.That(result.RowCount).IsEqualTo(SqliteTools.HardMaxRows);
            await Assert.That(result.Truncated).IsTrue();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
        }
    }

    [Test]
    public async Task ReturnsLastResultSetFromBatch()
    {
        using var temp = TempWorkspace.Create();
        var tool = CreateTool(temp);

        try
        {
            var result = await tool.Query(
                "batch.db",
                "CREATE TABLE items(id INTEGER, label TEXT); INSERT INTO items VALUES (1, 'first;keep'); SELECT 99 AS ignored; SELECT id, label FROM items;",
                cancellationToken: CancellationToken.None);

            await Assert.That(result.Error).IsNull();
            await Assert.That(result.RowCount).IsEqualTo(1);
            await Assert.That(result.Columns).IsEquivalentTo(["id", "label"]);
            await Assert.That(result.Rows[0]["label"]).IsEqualTo("first;keep");
            await Assert.That(result.RowsAffected).IsEqualTo(1);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
        }
    }

    [Test]
    public async Task RefusesWritesIntoSystemSubfolder()
    {
        using var temp = TempWorkspace.Create();
        var tool = CreateTool(temp);

        var result = await tool.Query("system/metrics.db", "SELECT 1;", cancellationToken: CancellationToken.None);

        await Assert.That(result.Error).IsNotNull().And.Contains("'system/'");
        await Assert.That(result.RowsAffected).IsEqualTo(0);
        await Assert.That(result.RowCount).IsEqualTo(0);
        await Assert.That(File.Exists(temp.PathOf("system", "metrics.db"))).IsFalse();
    }

    [Test]
    public async Task RefusesWritesOutsideWorkspace()
    {
        using var temp = TempWorkspace.Create();
        var tool = CreateTool(temp);

        var result = await tool.Query("../escape.db", "SELECT 1;", cancellationToken: CancellationToken.None);

        await Assert.That(result.Error).IsNotNull().And.Contains("outside the agent workspace");
        await Assert.That(result.RowsAffected).IsEqualTo(0);
        await Assert.That(result.Columns).IsEmpty();
        await Assert.That(result.Rows).IsEmpty();
        await Assert.That(result.RowCount).IsEqualTo(0);
        await Assert.That(result.Truncated).IsFalse();
    }

    [Test]
    public async Task HandlesSyntaxErrorGracefully()
    {
        using var temp = TempWorkspace.Create();
        var tool = CreateTool(temp);

        try
        {
            var result = await tool.Query("bad.db", "SELECT * FORM items;", cancellationToken: CancellationToken.None);

            await Assert.That(result.Error).IsNotNull().And.Contains("syntax error");
            await Assert.That(result.Path).IsEqualTo("bad.db");
            await Assert.That(result.RowsAffected).IsEqualTo(0);
            await Assert.That(result.Columns).IsEmpty();
            await Assert.That(result.Rows).IsEmpty();
            await Assert.That(result.RowCount).IsEqualTo(0);
            await Assert.That(result.Truncated).IsFalse();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
        }
    }

    [Test]
    public async Task RefusesWhenPathIsAnExistingDirectory()
    {
        using var temp = TempWorkspace.Create();
        Directory.CreateDirectory(temp.PathOf("state"));
        var tool = CreateTool(temp);

        var result = await tool.Query("state", "SELECT 1;", cancellationToken: CancellationToken.None);

        await Assert.That(result.Error).IsNotNull().And.Contains("directory");
        await Assert.That(result.RowCount).IsEqualTo(0);
    }

    private static SqliteTools CreateTool(TempWorkspace temp) =>
        new(
            new StubAgentWorkspaceLocator(temp.Workspace),
            new PathExpander(),
            TestFileProtectionPolicies.AllowAll,
            NullLogger<SqliteTools>.Instance);
}
