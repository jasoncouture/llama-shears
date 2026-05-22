using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Options;

namespace LlamaShears.Debugging;

public sealed class EventBusFileLogDecorator : IEventBus, IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
    };

    private readonly IEventBus _inner;
    private readonly EventBusFileLogOptions _options;
    private readonly TimeProvider _time;
    private readonly StreamWriter? _writer;
    private readonly Lock _writeLock = new Lock();
    private readonly ConcurrentDictionary<Type, bool> _serializesAsObject = new ConcurrentDictionary<Type, bool>();

    public EventBusFileLogDecorator(
        IEventBus inner,
        IOptions<EventBusFileLogOptions> options,
        TimeProvider time,
        IApplicationPathProvider paths)
    {
        _inner = inner;
        _options = options.Value;
        _time = time;
        if (!_options.Enabled) return;

        var dataRoot = paths.GetPath(PathKind.Data);
        var path = Path.IsPathRooted(_options.Filename)
            ? _options.Filename
            : Path.Combine(dataRoot, _options.Filename);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };
    }

    public IDisposable Subscribe<T>(
        string? pattern,
        EventDeliveryMode mode,
        IEventHandler<T> handler,
        bool preserveSubscriberExecutionContext = false)
        where T : class
    {
        WriteEntry("subscribe", pattern, data: null, dataType: typeof(T));
        return _inner.Subscribe(pattern, mode, handler, preserveSubscriberExecutionContext);
    }

    public ValueTask PublishAsync<T>(EventType eventType, T? data, Guid correlationId, CancellationToken cancellationToken)
        where T : class
    {
        WriteEntry("publish", eventType?.ToString(), data, typeof(T));
        return _inner.PublishAsync(eventType!, data, correlationId, cancellationToken);
    }

    private void WriteEntry(string action, string? eventType, object? data, Type dataType)
    {
        if (_writer is null) return;

        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("timestamp", _time.GetUtcNow().ToString("o"));
            json.WriteString("action", action);
            if (eventType is not null) json.WriteString("eventType", eventType);
            else json.WriteNull("eventType");
            WriteData(json, data, dataType);
            json.WriteEndObject();
        }

        lock (_writeLock)
        {
            _writer.Write(System.Text.Encoding.UTF8.GetString(buffer.ToArray()));
            _writer.Write('\n');
        }
    }

    private void WriteData(Utf8JsonWriter writer, object? data, Type dataType)
    {
        if (data is null)
        {
            writer.WriteNull("data");
            return;
        }

        if (_serializesAsObject.TryGetValue(dataType, out var asObject) && !asObject)
        {
            writer.WriteString("data", data.ToString());
            return;
        }

        try
        {
            var node = JsonSerializer.SerializeToNode(data, dataType, _jsonOptions);
            if (node is JsonObject)
            {
                _serializesAsObject.TryAdd(dataType, true);
                writer.WritePropertyName("data");
                node.WriteTo(writer, _jsonOptions);
                return;
            }
        }
        catch
        {
            // fall through to ToString fallback
        }

        _serializesAsObject[dataType] = false;
        writer.WriteString("data", data.ToString());
    }

    public void Dispose()
    {
        lock (_writeLock)
        {
            _writer?.Dispose();
        }
    }
}
