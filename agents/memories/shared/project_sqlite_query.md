---
name: sqlite_query is the native SQLite tool
description: Bundled MCP sqlite_query for workspace-confined SQL; WAL + busy_timeout; do not send agents to sqlite3 via shell
type: project
---

`sqlite_query` (`LlamaShears.Api.Tools.ModelContextProtocol.Sqlite`) is the first-class SQLite client. Agents should use it instead of `shell_run` + `sqlite3`.

- Path is workspace-relative. Same write confinement as `file_write` (`WorkspacePathResolver.ResolveForWrite`, `system/` banned, protection policy). Missing parents and the `.db` file are created on open.
- `sql` may be a multi-statement batch. Microsoft.Data.Sqlite runs only the first statement, so the tool splits on `;` outside quotes/comments and executes each. The last result set wins; `rowsAffected` is aggregated.
- `maxRows` defaults to 100, hard cap 1000. Stop the reader at the cap (`truncated=true`); do not buffer unbounded rows.
- Types: INTEGER → long, REAL → double, TEXT → string, NULL → null, BLOB → Base64. Convert `DateTime` to `DateTimeOffset` at the boundary.
- Connections: `ReadWriteCreate` + shared cache, then `PRAGMA journal_mode = WAL` and `PRAGMA busy_timeout = 5000`. SQL errors return JSON `error` — they must not throw out of the tool call.
- Tests that open a file must `SqliteConnection.ClearAllPools()` before deleting the temp workspace.

This is not a security sandbox. Agents already have `shell_run`. MCP allow/deny of the tool is the gate.
