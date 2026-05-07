using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Plugins.Host;

/// <summary>
/// One captured log call. <see cref="Format"/> follows the .NET
/// <c>ILogger</c> message-template convention; <see cref="Data"/>
/// carries the corresponding positional arguments.
/// </summary>
public record struct DeferredLogEntry(
    LogLevel Level,
    string Format,
    Exception? Exception,
    ImmutableArray<object?> Data);
