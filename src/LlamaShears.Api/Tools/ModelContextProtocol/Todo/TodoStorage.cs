using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Abstractions.Caching;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Todo;

internal sealed partial class TodoStorage : ITodoStorage
{
    private const int MaxTextLength = 200;
    private const string FileName = "TODO.md";
    private static readonly string _header =
        """
        # Agent TODO List
        IMPORTANT: This file is auto-managed, and any changes will either be lost, or result in the list being cleared if it cannot be parsed.

        ## TODO


        """.Replace("\r\n", "\n");

    [GeneratedRegex(@"^(?<index>\d+)\.\s+\[(?<state>[ x])\]\s+(?<text>.+)$")]
    private static partial Regex ItemPattern();

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IFileParserCache<TodoStorage> _cache;
    private readonly ILogger<TodoStorage> _logger;

    public TodoStorage(
        IAgentWorkspaceLocator workspace,
        IFileParserCache<TodoStorage> cache,
        ILogger<TodoStorage> logger)
    {
        _workspace = workspace;
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<TodoCommandResult> ListAsync(int? offset = default, int? limit = default, CancellationToken cancellationToken = default)
    {
        var path = await GetPathAsync(cancellationToken).ConfigureAwait(false);
        var parsed = await ParseAsync(path, cancellationToken).ConfigureAwait(false);
        if (parsed.Corrupt)
        {
            await WriteHeaderOnlyAsync(path, cancellationToken).ConfigureAwait(false);
            LogRecovered(_logger, path);
            return new TodoCommandResult([], TodoResultState.Corrupt);
        }

        var items = parsed.Items;
        if (offset is int skip && skip > 0)
        {
            items = [.. items.Skip(skip)];
        }
        if (limit is int take && take >= 0)
        {
            items = [.. items.Take(take)];
        }
        return new TodoCommandResult(items, TodoResultState.Success);
    }

    public async ValueTask<TodoCommandResult> AddAsync(string text, bool done = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TodoCommandResult([], TodoResultState.Refused, "Todo item text is required");
        }
        if (text.Contains('\n', StringComparison.Ordinal) || text.Contains('\r', StringComparison.Ordinal))
        {
            return new TodoCommandResult([], TodoResultState.Refused, "Todo item text cannot contain new lines");
        }
        if (text.Length > MaxTextLength)
        {
            return new TodoCommandResult([], TodoResultState.Refused, $"Todo item text cannot be more than {MaxTextLength} characters");
        }

        var path = await GetPathAsync(cancellationToken).ConfigureAwait(false);
        var parsed = await ParseAsync(path, cancellationToken).ConfigureAwait(false);
        if (parsed.Corrupt)
        {
            var recovered = new TodoItem(1, text, done);
            await File.WriteAllTextAsync(path, $"{_header}{recovered}\n", cancellationToken).ConfigureAwait(false);
            LogRecovered(_logger, path);
            return new TodoCommandResult([recovered], TodoResultState.Corrupt);
        }

        var nextIndex = parsed.Items.IsEmpty ? 1 : parsed.Items[^1].Index + 1;
        var newItem = new TodoItem(nextIndex, text, done);

        if (parsed.Items.IsEmpty)
        {
            await File.WriteAllTextAsync(path, $"{_header}{newItem}\n", cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await File.AppendAllTextAsync(path, $"{newItem}\n", cancellationToken).ConfigureAwait(false);
        }
        return new TodoCommandResult(parsed.Items.Add(newItem), TodoResultState.Success);
    }

    public async ValueTask<TodoCommandResult> UpdateAsync(int index, bool isCompleted, CancellationToken cancellationToken = default)
    {
        var path = await GetPathAsync(cancellationToken).ConfigureAwait(false);
        var parsed = await ParseAsync(path, cancellationToken).ConfigureAwait(false);
        if (parsed.Corrupt)
        {
            await WriteHeaderOnlyAsync(path, cancellationToken).ConfigureAwait(false);
            LogRecovered(_logger, path);
            return new TodoCommandResult([], TodoResultState.Corrupt, $"No todo item at index {index}.");
        }

        var existing = FindByIndex(parsed.Items, index);
        if (existing is null)
        {
            return new TodoCommandResult(parsed.Items, TodoResultState.Refused, $"No todo item at index {index}.");
        }
        if (existing.Completed == isCompleted)
        {
            return new TodoCommandResult(parsed.Items, TodoResultState.Success);
        }

        var updated = parsed.Items.Replace(existing, existing with { Completed = isCompleted });
        await RewriteAsync(path, updated, cancellationToken).ConfigureAwait(false);
        return new TodoCommandResult(updated, TodoResultState.Success);
    }

    public async ValueTask<TodoCommandResult> DeleteAsync(int index, CancellationToken cancellationToken = default)
    {
        var path = await GetPathAsync(cancellationToken).ConfigureAwait(false);
        var parsed = await ParseAsync(path, cancellationToken).ConfigureAwait(false);
        if (parsed.Corrupt)
        {
            await WriteHeaderOnlyAsync(path, cancellationToken).ConfigureAwait(false);
            LogRecovered(_logger, path);
            return new TodoCommandResult([], TodoResultState.Corrupt, $"No todo item at index {index}.");
        }

        var existing = FindByIndex(parsed.Items, index);
        if (existing is null)
        {
            return new TodoCommandResult(parsed.Items, TodoResultState.Refused, $"No todo item at index {index}.");
        }

        var renumbered = Renumber(parsed.Items.Remove(existing));
        await RewriteAsync(path, renumbered, cancellationToken).ConfigureAwait(false);
        return new TodoCommandResult(renumbered, TodoResultState.Success);
    }

    public async ValueTask<TodoCommandResult> ClearAsync(bool includeCompleted, CancellationToken cancellationToken = default)
    {
        var path = await GetPathAsync(cancellationToken).ConfigureAwait(false);
        var parsed = await ParseAsync(path, cancellationToken).ConfigureAwait(false);
        if (parsed.Corrupt)
        {
            await WriteHeaderOnlyAsync(path, cancellationToken).ConfigureAwait(false);
            LogRecovered(_logger, path);
            return new TodoCommandResult([], TodoResultState.Corrupt);
        }
        if (parsed.Items.IsEmpty)
        {
            return new TodoCommandResult([], TodoResultState.Success);
        }

        var kept = includeCompleted
            ? ImmutableArray<TodoItem>.Empty
            : [.. parsed.Items.Where(static item => !item.Completed)];
        if (kept.Length == parsed.Items.Length)
        {
            return new TodoCommandResult(parsed.Items, TodoResultState.Success);
        }

        var renumbered = Renumber(kept);
        await RewriteAsync(path, renumbered, cancellationToken).ConfigureAwait(false);
        return new TodoCommandResult(renumbered, TodoResultState.Success);
    }

    private async ValueTask<string> GetPathAsync(CancellationToken cancellationToken)
    {
        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        return Path.Combine(workspace.Root, FileName);
    }

    private async ValueTask<ParsedList> ParseAsync(string path, CancellationToken cancellationToken)
    {
        var state = new ParseState(path);
        var result = await _cache.GetOrParseAsync(
            path, state, ParseFile, cancellationToken).ConfigureAwait(false);
        return result ?? new ParsedList([], Corrupt: false);
    }

    private static async ValueTask<ParsedList?> ParseFile(Stream? stream, ParseState state, CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            return new ParsedList([], Corrupt: false);
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var items = ImmutableArray.CreateBuilder<TodoItem>();
        var inList = false;
        var sawTrailingBlank = false;
        var corrupt = false;
        var pattern = ItemPattern();

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            if (corrupt)
            {
                break;
            }
            var match = pattern.Match(line);
            if (!inList)
            {
                if (!match.Success)
                {
                    continue;
                }
                inList = true;
                items.Add(BuildItem(match));
                continue;
            }
            if (string.IsNullOrEmpty(line))
            {
                sawTrailingBlank = true;
                continue;
            }
            if (!match.Success || sawTrailingBlank)
            {
                corrupt = true;
                continue;
            }
            items.Add(BuildItem(match));
        }
        if (corrupt)
        {
            return new ParsedList([], Corrupt: true);
        }

        var sorted = items.ToImmutable().Sort(static (a, b) => a.Index.CompareTo(b.Index));
        var seen = new HashSet<int>();
        var deduped = ImmutableArray.CreateBuilder<TodoItem>();
        foreach (var item in sorted)
        {
            if (seen.Add(item.Index))
            {
                deduped.Add(item);
            }
        }
        return new ParsedList(deduped.ToImmutable(), Corrupt: false);
    }

    private static TodoItem BuildItem(Match match)
    {
        var index = int.Parse(match.Groups["index"].ValueSpan);
        var done = match.Groups["state"].Value == "x";
        var text = match.Groups["text"].Value;
        return new TodoItem(index, text, done);
    }

    private static TodoItem? FindByIndex(ImmutableArray<TodoItem> items, int index)
    {
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i].Index == index)
            {
                return items[i];
            }
        }
        return null;
    }

    private static ImmutableArray<TodoItem> Renumber(ImmutableArray<TodoItem> items)
    {
        if (items.IsEmpty)
        {
            return [];
        }
        var builder = ImmutableArray.CreateBuilder<TodoItem>();
        for (var i = 0; i < items.Length; i++)
        {
            builder.Add(items[i] with { Index = i + 1 });
        }
        return builder.ToImmutable();
    }

    private static Task RewriteAsync(string path, ImmutableArray<TodoItem> items, CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder(_header);
        foreach (var item in items)
        {
            stringBuilder.Append(item.ToString()).Append('\n');
        }
        return File.WriteAllTextAsync(path, stringBuilder.ToString(), cancellationToken);
    }

    private static Task WriteHeaderOnlyAsync(string path, CancellationToken cancellationToken)
        => File.WriteAllTextAsync(path, _header, cancellationToken);

    [LoggerMessage(Level = LogLevel.Warning, Message = "TODO list at '{Path}' was corrupt; reset to empty.")]
    private static partial void LogRecovered(ILogger logger, string path);

    private readonly record struct ParseState(string Path);
    private sealed record ParsedList(ImmutableArray<TodoItem> Items, bool Corrupt);
}
