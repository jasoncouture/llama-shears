using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Api.Web.Services;

public sealed partial class IconProvider : IIconProvider
{
    private const string IconRoot = "_content/LlamaShears.Api.Web/icons";

    private readonly IFileProvider _files;
    private readonly ILogger<IconProvider> _logger;
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);

    public IconProvider(IWebHostEnvironment environment, ILogger<IconProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);
        _files = environment.WebRootFileProvider;
        _logger = logger;
    }

    public string GetInnerSvg(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _cache.GetOrAdd(name, Load);
    }

    private string Load(string name)
    {
        var path = $"{IconRoot}/{name}.svg";
        var fileInfo = _files.GetFileInfo(path);
        if (!fileInfo.Exists)
        {
            LogIconMissing(_logger, name, path);
            return string.Empty;
        }

        using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        var inner = ExtractInner(content);
        if (inner.Length == 0)
        {
            LogIconMalformed(_logger, name, path);
        }
        return inner;
    }

    private static string ExtractInner(string svg)
    {
        var openEnd = svg.IndexOf('>');
        if (openEnd < 0)
        {
            return string.Empty;
        }

        var closeStart = svg.LastIndexOf("</svg>", StringComparison.OrdinalIgnoreCase);
        if (closeStart < 0 || closeStart <= openEnd)
        {
            return string.Empty;
        }

        return svg[(openEnd + 1)..closeStart].Trim();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Icon '{Name}' not found at {Path}.")]
    private static partial void LogIconMissing(ILogger logger, string name, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Icon '{Name}' at {Path} did not parse — no inner svg body.")]
    private static partial void LogIconMalformed(ILogger logger, string name, string path);
}
