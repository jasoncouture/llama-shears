using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using StrangeSoft.Plugins.Host;

namespace LlamaShears.Plugins.Host;

/// <summary>
/// Buffers plugin-host log calls into an in-memory queue so they can
/// be handed off to a real logger once one exists (typically after
/// DI is built). Entries are recorded in arrival order; concurrent
/// callers serialize on a monitor lock.
/// </summary>
public sealed class DeferredPluginHostLogger : IPluginContextLogger
{
    private readonly object _gate = new object();
    private readonly List<DeferredLogEntry> _entries = [];

    public void Debug(string format, params IEnumerable<object?> data)
        => Append(LogLevel.Debug, format, exception: null, data);

    public void Information(string format, params IEnumerable<object?> data)
        => Append(LogLevel.Information, format, exception: null, data);

    public void Warning(string format, Exception? exception, params IEnumerable<object?> data)
        => Append(LogLevel.Warning, format, exception, data);

    public void Error(string format, Exception? exception, params IEnumerable<object?> data)
        => Append(LogLevel.Error, format, exception, data);

    /// <summary>
    /// Atomically returns and clears the buffered entries in arrival
    /// order. The caller is responsible for forwarding them to whatever
    /// real logging stack is now available.
    /// </summary>
    public ImmutableArray<DeferredLogEntry> Drain()
    {
        if (_logger is not null)
        {
            return [];
        }
        lock (_gate)
        {
            var snapshot = _entries.ToImmutableArray();
            _entries.Clear();
            return snapshot;
        }
    }

    private ILogger? _logger;

    public void RedirectTo(ILogger logger)
    {
        foreach (var deferred in Drain())
        {
            WriteEntry(logger, deferred);
        }

        _logger = logger;
        ImmutableArray<DeferredLogEntry> finalEntries;
        lock (_gate)
        {
            finalEntries = [.. _entries];
            _entries.Clear();
        }

        foreach (var deferred in finalEntries)
        {
            WriteEntry(logger, deferred);
        }
    }

    private static void WriteEntry(ILogger logger, DeferredLogEntry entry)
    {
#pragma warning disable CA1873 // Avoid potentially expensive logging
        logger.Log(entry.Level, entry.Exception, entry.Format, [.. entry.Data]);
#pragma warning restore CA1873 // Avoid potentially expensive logging
    }

    private void Append(LogLevel level, string format, Exception? exception, IEnumerable<object?> data)
    {

        var entry = new DeferredLogEntry(level, format, exception, [.. data]);
        if(_logger is not null)
        {
            WriteEntry(_logger, entry);
            return;
        }
        lock (_gate)
        {
            if (_logger is null)
            {
                _entries.Add(entry);
                return;
            }
        }
        WriteEntry(_logger, entry);
    }
}
