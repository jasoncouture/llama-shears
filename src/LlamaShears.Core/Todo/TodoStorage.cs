using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Todo;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Common;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Todo;

internal sealed partial class TodoStorage : ITodoStorage
{
    private const int MaxTextLength = 200;
    private const string FileName = "TODO.md";

    [GeneratedRegex(@"^(?<index>\d+)\.\s+\[(?<state>[ x])\]\s+(?<text>.+)$")]
    private static partial Regex ItemPattern();

    private readonly IDataContextScope _scope;
    private readonly IFileParserCache<TodoStorage> _cache;
    private readonly ILogger<TodoStorage> _logger;

    public TodoStorage(
        IDataContextScope scope,
        IFileParserCache<TodoStorage> cache,
        ILogger<TodoStorage> logger)
    {
        _scope = scope;
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<TodoCommandResult> ListAsync(int? offset = default, int? limit = default, CancellationToken cancellationToken = default)
    {
        var path = await GetPathAsync(cancellationToken);
        var parsed = await ParseAsync(path, cancellationToken);
        if (parsed.Corrupt)
        {
            await WriteEmptyAsync(path, cancellationToken);
            LogRecovered(path);
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

    public async ValueTask<TodoCommandResult> AddAsync(IReadOnlyList<string> items, bool done = false, CancellationToken cancellationToken = default)
    {
        if (items is null || items.Count == 0)
        {
            return new TodoCommandResult([], TodoResultState.Refused, "At least one todo item is required");
        }
        for (var i = 0; i < items.Count; i++)
        {
            var text = items[i];
            if (string.IsNullOrWhiteSpace(text))
            {
                return new TodoCommandResult([], TodoResultState.Refused, $"Todo item text is required (items[{i}])");
            }
            if (text.Contains('\n', StringComparison.Ordinal) || text.Contains('\r', StringComparison.Ordinal))
            {
                return new TodoCommandResult([], TodoResultState.Refused, $"Todo item text cannot contain new lines (items[{i}])");
            }
            if (text.Length > MaxTextLength)
            {
                return new TodoCommandResult([], TodoResultState.Refused, $"Todo item text cannot be more than {MaxTextLength} characters (items[{i}])");
            }
        }

        var path = await GetPathAsync(cancellationToken);
        var parsed = await ParseAsync(path, cancellationToken);
        if (parsed.Corrupt)
        {
            var recovered = BuildBatch(items, done, startIndex: 1);
            await RewriteAsync(path, recovered, cancellationToken);
            LogRecovered(path);
            return new TodoCommandResult(recovered, TodoResultState.Corrupt);
        }

        var startIndex = parsed.Items.IsEmpty ? 1 : parsed.Items[^1].Index + 1;
        var newItems = BuildBatch(items, done, startIndex);

        ImmutableArray<TodoItem> finalItems;
        if (parsed.Items.IsEmpty)
        {
            finalItems = parsed.Items.AddRange(newItems);
            await RewriteAsync(path, finalItems, cancellationToken);
        }
        else
        {
            finalItems = await AppendAsync(path, newItems, cancellationToken);
        }
        return new TodoCommandResult(finalItems, TodoResultState.Success);
    }

    public async ValueTask<TodoCommandResult> UpdateAsync(IReadOnlyList<TodoItemUpdate> updates, CancellationToken cancellationToken = default)
    {
        if (updates is null || updates.Count == 0)
        {
            return new TodoCommandResult([], TodoResultState.Refused, "At least one update is required");
        }

        var path = await GetPathAsync(cancellationToken);
        var parsed = await ParseAsync(path, cancellationToken);
        if (parsed.Corrupt)
        {
            await WriteEmptyAsync(path, cancellationToken);
            LogRecovered(path);
            return new TodoCommandResult([], TodoResultState.Corrupt, "No todo items to update.");
        }

        var working = parsed.Items.ToBuilder();
        var changed = false;
        foreach (var update in updates)
        {
            var slot = IndexOfItem(working, update.Index);
            if (slot < 0)
            {
                return new TodoCommandResult(parsed.Items, TodoResultState.Refused, $"No todo item at index {update.Index}.");
            }
            if (working[slot].Completed == update.IsCompleted)
            {
                continue;
            }
            working[slot] = working[slot] with { Completed = update.IsCompleted };
            changed = true;
        }

        if (!changed)
        {
            return new TodoCommandResult(parsed.Items, TodoResultState.Success);
        }

        var updated = working.ToImmutable();
        await RewriteAsync(path, updated, cancellationToken);
        PushToScope(updated);
        return new TodoCommandResult(updated, TodoResultState.Success);
    }

    public async ValueTask<TodoCommandResult> DeleteAsync(IReadOnlyList<int> indices, CancellationToken cancellationToken = default)
    {
        if (indices is null || indices.Count == 0)
        {
            return new TodoCommandResult([], TodoResultState.Refused, "At least one index is required");
        }

        var path = await GetPathAsync(cancellationToken);
        var parsed = await ParseAsync(path, cancellationToken);
        if (parsed.Corrupt)
        {
            await WriteEmptyAsync(path, cancellationToken);
            LogRecovered(path);
            PushToScope([]);
            return new TodoCommandResult([], TodoResultState.Corrupt, "No todo items to delete.");
        }

        var targets = new HashSet<int>();
        foreach (var index in indices)
        {
            if (FindByIndex(parsed.Items, index) is null)
            {
                return new TodoCommandResult(parsed.Items, TodoResultState.Refused, $"No todo item at index {index}.");
            }
            targets.Add(index);
        }

        var remaining = parsed.Items.Where(item => !targets.Contains(item.Index)).ToImmutableArray();
        var renumbered = Renumber(remaining);
        await RewriteAsync(path, renumbered, cancellationToken);
        PushToScope(renumbered);
        return new TodoCommandResult(renumbered, TodoResultState.Success);
    }

    public async ValueTask<TodoCommandResult> ClearAsync(bool includeIncomplete, CancellationToken cancellationToken = default)
    {
        var path = await GetPathAsync(cancellationToken);
        var parsed = await ParseAsync(path, cancellationToken);
        if (parsed.Corrupt)
        {
            await WriteEmptyAsync(path, cancellationToken);
            LogRecovered(path);
            PushToScope([]);
            return new TodoCommandResult([], TodoResultState.Corrupt);
        }
        if (parsed.Items.IsEmpty)
        {
            return new TodoCommandResult([], TodoResultState.Success);
        }

        var kept = includeIncomplete
            ? ImmutableArray<TodoItem>.Empty
            : [.. parsed.Items.Where(static item => !item.Completed)];
        if (kept.Length == parsed.Items.Length)
        {
            return new TodoCommandResult(parsed.Items, TodoResultState.Success);
        }

        var renumbered = Renumber(kept);
        await RewriteAsync(path, renumbered, cancellationToken);
        PushToScope(renumbered);
        return new TodoCommandResult(renumbered, TodoResultState.Success);
    }

    private void PushToScope(ImmutableArray<TodoItem> items)
    {
        _scope.SetItem(TodoStorageConstants.DataKey, items);
    }

    private ValueTask<string> GetPathAsync(CancellationToken cancellationToken)
    {
        var config = _scope.GetAgentConfig();
        var root = string.IsNullOrEmpty(config.WorkspacePath)
            ? Environment.CurrentDirectory
            : config.WorkspacePath;
        return ValueTask.FromResult(Path.Combine(root, FileName));
    }

    private async ValueTask<ParsedList> ParseAsync(string path, CancellationToken cancellationToken)
    {
        var state = new ParseState(path);
        var result = await _cache.GetOrParseAsync(
            path, state, ParseFile, cancellationToken);
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

    private static int IndexOfItem(ImmutableArray<TodoItem>.Builder items, int index)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Index == index)
            {
                return i;
            }
        }
        return -1;
    }

    private static ImmutableArray<TodoItem> BuildBatch(IReadOnlyList<string> items, bool done, int startIndex)
    {
        var builder = ImmutableArray.CreateBuilder<TodoItem>();
        for (var i = 0; i < items.Count; i++)
        {
            builder.Add(new TodoItem(startIndex + i, items[i], done));
        }
        return builder.ToImmutable();
    }

    private static string RenderItems(ImmutableArray<TodoItem> items)
    {
        var stringBuilder = new StringBuilder();
        foreach (var item in items)
        {
            stringBuilder.Append(item).Append('\n');
        }
        return stringBuilder.ToString();
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

    private async Task RewriteAsync(string path, ImmutableArray<TodoItem> items, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(path, RenderItems(items), cancellationToken);
        PushToScope(items);
    }

    private async Task<ImmutableArray<TodoItem>> AppendAsync(string path, ImmutableArray<TodoItem> newItems, CancellationToken cancellationToken)
    {
        await File.AppendAllTextAsync(path, RenderItems(newItems), cancellationToken);
        var refreshed = await ParseAsync(path, cancellationToken);
        var items = refreshed.Corrupt ? [] : refreshed.Items;
        PushToScope(items);
        return items;
    }

    private Task WriteEmptyAsync(string path, CancellationToken cancellationToken)
        => RewriteAsync(path, [], cancellationToken);

    [LoggerMessage(Level = LogLevel.Warning, Message = "TODO list at '{Path}' was corrupt; reset to empty.")]
    private partial void LogRecovered(string path);

    private readonly record struct ParseState(string Path);
    private sealed record ParsedList(ImmutableArray<TodoItem> Items, bool Corrupt);
}
