using System.Collections.Immutable;
using System.Text;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Sqlite;

internal static class SqliteBatchSplitter
{
    public static ImmutableArray<string> Split(string sql)
    {
        var statements = new List<string>();
        var current = new StringBuilder();
        var i = 0;
        while (i < sql.Length)
        {
            var c = sql[i];
            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                AppendLineComment(sql, current, ref i);
                continue;
            }

            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                AppendBlockComment(sql, current, ref i);
                continue;
            }

            if (c is '\'' or '"' or '`' or '[')
            {
                AppendQuoted(sql, current, ref i, c);
                continue;
            }

            if (c == ';')
            {
                Flush(statements, current);
                i++;
                continue;
            }

            current.Append(c);
            i++;
        }

        Flush(statements, current);
        return [.. statements];
    }

    private static void AppendLineComment(string sql, StringBuilder current, ref int i)
    {
        while (i < sql.Length)
        {
            var c = sql[i];
            current.Append(c);
            i++;
            if (c == '\n')
            {
                break;
            }
        }
    }

    private static void AppendBlockComment(string sql, StringBuilder current, ref int i)
    {
        current.Append(sql[i]);
        current.Append(sql[i + 1]);
        i += 2;
        while (i < sql.Length)
        {
            var c = sql[i];
            current.Append(c);
            i++;
            if (c == '*' && i < sql.Length && sql[i] == '/')
            {
                current.Append(sql[i]);
                i++;
                break;
            }
        }
    }

    private static void AppendQuoted(string sql, StringBuilder current, ref int i, char opener)
    {
        var closer = opener == '[' ? ']' : opener;
        current.Append(sql[i]);
        i++;
        while (i < sql.Length)
        {
            var c = sql[i];
            current.Append(c);
            i++;
            if (c != closer)
            {
                continue;
            }

            if (opener != '[' && i < sql.Length && sql[i] == closer)
            {
                current.Append(sql[i]);
                i++;
                continue;
            }

            break;
        }
    }

    private static void Flush(List<string> statements, StringBuilder current)
    {
        var statement = current.ToString().Trim();
        current.Clear();
        if (statement.Length > 0)
        {
            statements.Add(statement);
        }
    }
}
