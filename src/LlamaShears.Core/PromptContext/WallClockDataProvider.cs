using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.PromptContext;

internal sealed class WallClockDataProvider : IDataContextItemProvider
{
    private readonly TimeProvider _time;

    public WallClockDataProvider(TimeProvider time)
    {
        _time = time;
    }

    public Task<IEnumerable<KeyValuePair<string, object?>>> GetItemsForCurrentContext(
        CancellationToken cancellationToken = default)
    {
        var now = _time.GetLocalNow();
        IEnumerable<KeyValuePair<string, object?>> items =
        [
            new KeyValuePair<string, object?>("now", now),
            new KeyValuePair<string, object?>("timezone", TimeZoneInfo.Local.Id),
            new KeyValuePair<string, object?>("day_of_week", now.DayOfWeek.ToString()),
        ];
        return Task.FromResult(items);
    }
}
