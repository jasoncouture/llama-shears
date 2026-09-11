using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Http;

[McpServerToolType]
public sealed partial class HttpTools
{
    public const string HttpClientName = "HttpRequest";
    public const int DefaultTimeoutSeconds = 30;
    public const int MinTimeoutSeconds = 1;
    public const int MaxTimeoutSeconds = 120;
    public const int MaxRequestBodyBytes = 256 * 1024;
    public const int MaxResponseBodyBytes = 64 * 1024;
    public const int MaxSaveBytes = 16 * 1024 * 1024;
    public const int MaxHeaderCount = 32;

    private static readonly HashSet<string> _allowedMethods =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "POST", "PUT", "PATCH", "DELETE" };

    private static readonly HashSet<string> _forbiddenHeaders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Host",
            "Content-Length",
            "Transfer-Encoding",
            "Connection",
            "Keep-Alive",
            "Upgrade",
            "TE",
            "Trailer",
            "Proxy-Connection",
        };

    private static readonly HashSet<string> _contentHeaders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Content-Type",
            "Content-Encoding",
            "Content-Language",
            "Content-Disposition",
        };

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IPathExpander _pathExpander;
    private readonly IFileProtectionPolicy _protection;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpTools> _logger;

    public HttpTools(
        IAgentWorkspaceLocator workspace,
        IPathExpander pathExpander,
        IFileProtectionPolicy protection,
        IHttpClientFactory httpClientFactory,
        ILogger<HttpTools> logger)
    {
        _workspace = workspace;
        _pathExpander = pathExpander;
        _protection = protection;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [McpServerTool(Name = "http_request", Destructive = false, OpenWorld = true)]
    [Description("Performs an HTTP request and returns status, reasonPhrase, response headers, contentType, and body as JSON. Use this instead of shell_run + curl. Allowed methods: GET, HEAD, POST, PUT, PATCH, DELETE. URL must be http or https. Optional headers are a string map (do not set Host or Content-Length). Optional body is raw text; GET/HEAD refuse a body. If body is set and Content-Type is omitted, application/json is used when the body looks like JSON, otherwise text/plain. Timeout defaults to 30s (max 120). Response bodies inline are capped at 64 KiB (truncated=true) and omitted when binary (binary=true). Pass saveAs to write the full body (text or binary) into the workspace — same write confinement as file_write (no system/, no escape, protection policy). Save cap is 16 MiB (savedTruncated=true if cut). Existing files are refused unless overwrite=true. HTTP error statuses complete the call with ok=false — they are not tool failures. Transport/timeout failures set error.")]
    public async Task<HttpRequestResult> Request(
        [Description("Absolute http or https URL.")]
        string url,
        [Description("HTTP method. Defaults to GET.")]
        string method = "GET",
        [Description("Optional request headers as a name→value map. Do not set Host, Content-Length, Transfer-Encoding, or Connection.")]
        Dictionary<string, string>? headers = null,
        [Description("Optional raw request body. Required for most POST/PUT/PATCH calls. Forbidden on GET/HEAD.")]
        string? body = null,
        [Description("Per-call timeout in seconds. Defaults to 30. Minimum 1, maximum 120.")]
        int timeoutSeconds = DefaultTimeoutSeconds,
        [Description("Optional workspace-relative path to save the response body (use this for binaries, images, PDFs, or bodies larger than the inline cap). Absolute paths must still resolve inside the workspace.")]
        string? saveAs = null,
        [Description("If true, replace an existing file when saveAs is set. Defaults to false.")]
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return Fail("Refused: http_request requires an authenticated agent on the request.", url);
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return Fail("Refused: url is required.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Fail("Refused: url must be an absolute http or https URI.", url);
        }

        if (string.IsNullOrWhiteSpace(method) || !_allowedMethods.Contains(method))
        {
            return Fail(
                $"Refused: method '{method}' is not allowed. Use GET, HEAD, POST, PUT, PATCH, or DELETE.",
                url);
        }

        var verb = method.ToUpperInvariant();
        if (timeoutSeconds is < MinTimeoutSeconds or > MaxTimeoutSeconds)
        {
            return Fail(
                $"Refused: timeoutSeconds must be between {MinTimeoutSeconds} and {MaxTimeoutSeconds}.",
                url);
        }

        var hasBody = body is not null;
        if (hasBody && verb is "GET" or "HEAD")
        {
            return Fail($"Refused: {verb} cannot include a body.", url);
        }

        if (hasBody && Encoding.UTF8.GetByteCount(body!) > MaxRequestBodyBytes)
        {
            return Fail(
                $"Refused: request body is {Encoding.UTF8.GetByteCount(body!)} bytes; maximum is {MaxRequestBodyBytes}.",
                url);
        }

        if (headers is { Count: > MaxHeaderCount })
        {
            return Fail($"Refused: at most {MaxHeaderCount} headers are allowed.", url);
        }

        if (headers is not null)
        {
            foreach (var name in headers.Keys)
            {
                if (_forbiddenHeaders.Contains(name))
                {
                    return Fail($"Refused: header '{name}' cannot be set by the caller.", url);
                }
            }
        }

        string? savePath = null;
        if (!string.IsNullOrWhiteSpace(saveAs))
        {
            var resolution = WorkspacePathResolver.ResolveForWrite(workspace, saveAs);
            if (!resolution.IsSuccess)
            {
                return Fail(resolution.Error, url);
            }

            if (Directory.Exists(resolution.FullPath))
            {
                return Fail($"Refused: '{saveAs}' is an existing directory.", url);
            }

            var expanded = _pathExpander.ExpandPath(saveAs, workspace.Root);
            var protection = _protection.Match(workspace.Root, expanded, FileType.File, ProtectionMode.Write);
            if (protection is not null)
            {
                return Fail(ProtectionRefusal.Format(saveAs, ProtectionMode.Write, protection), url);
            }

            if (File.Exists(resolution.FullPath) && !overwrite)
            {
                return Fail($"Refused: '{saveAs}' already exists. Pass overwrite=true to replace it.", url);
            }

            savePath = resolution.FullPath;
        }

        using var request = new HttpRequestMessage(new HttpMethod(verb), uri);
        if (hasBody)
        {
            request.Content = new StringContent(body!, Encoding.UTF8);
            var contentType = FindHeader(headers, "Content-Type");
            if (contentType is null)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(LooksLikeJson(body!) ? "application/json" : "text/plain")
                {
                    CharSet = "utf-8",
                };
            }
        }

        if (headers is not null)
        {
            foreach (var (name, value) in headers)
            {
                if (_contentHeaders.Contains(name))
                {
                    request.Content ??= new StringContent(string.Empty, Encoding.UTF8);
                    request.Content.Headers.Remove(name);
                    if (!request.Content.Headers.TryAddWithoutValidation(name, value))
                    {
                        return Fail($"Refused: header '{name}' is not a valid content header value.", url);
                    }
                }
                else if (!request.Headers.TryAddWithoutValidation(name, value))
                {
                    return Fail($"Refused: header '{name}' is not a valid request header value.", url);
                }
            }
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var clock = Stopwatch.StartNew();
        try
        {
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            var read = await ReadBodyAsync(response, savePath, timeout.Token);
            clock.Stop();
            var elapsed = (int)Math.Min(clock.ElapsedMilliseconds, int.MaxValue);
            if (read.SaveError is not null)
            {
                LogSaveFailed(workspace.AgentId, saveAs!, read.SaveError);
            }
            else if (savePath is not null)
            {
                LogSaved(workspace.AgentId, saveAs!, read.SavedBytes ?? 0, read.SavedTruncated);
            }

            LogCompleted(workspace.AgentId, verb, uri.Host, (int)response.StatusCode, read.Truncated, read.Binary);
            return new HttpRequestResult(
                Completed: true,
                Status: (int)response.StatusCode,
                ReasonPhrase: response.ReasonPhrase,
                Ok: response.IsSuccessStatusCode,
                Url: response.RequestMessage?.RequestUri?.ToString() ?? uri.ToString(),
                Headers: FlattenHeaders(response),
                ContentType: read.ContentType,
                Body: read.Body,
                Truncated: read.Truncated,
                Binary: read.Binary,
                TimedOut: false,
                ElapsedMilliseconds: elapsed,
                SavedPath: read.SaveError is null ? saveAs : null,
                SavedBytes: read.SaveError is null ? read.SavedBytes : null,
                SavedTruncated: read.SavedTruncated,
                Error: read.SaveError);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            clock.Stop();
            LogTimedOut(workspace.AgentId, verb, uri.Host, timeoutSeconds);
            return Fail(
                $"Request timed out after {timeoutSeconds}s.",
                uri.ToString(),
                timedOut: true,
                elapsed: (int)Math.Min(clock.ElapsedMilliseconds, int.MaxValue));
        }
        catch (Exception ex)
        {
            clock.Stop();
            LogFailed(workspace.AgentId, verb, uri.Host, ex);
            return Fail(
                $"Failed to perform HTTP request: {ex.Message}",
                uri.ToString(),
                elapsed: (int)Math.Min(clock.ElapsedMilliseconds, int.MaxValue));
        }
    }

    private static HttpRequestResult Fail(
        string error,
        string? url = null,
        bool timedOut = false,
        int elapsed = 0)
    {
        return new HttpRequestResult(
            Completed: false,
            Status: null,
            ReasonPhrase: null,
            Ok: false,
            Url: url,
            Headers: [],
            ContentType: null,
            Body: null,
            Truncated: false,
            Binary: false,
            TimedOut: timedOut,
            ElapsedMilliseconds: elapsed,
            Error: error);
    }

    private static string? FindHeader(Dictionary<string, string>? headers, string name)
    {
        if (headers is null)
        {
            return null;
        }

        foreach (var (key, value) in headers)
        {
            if (key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    private static bool LooksLikeJson(string body)
    {
        var trimmed = body.AsSpan().Trim();
        return trimmed.Length > 0 && trimmed[0] is '{' or '[';
    }

    private static async Task<HttpBodyRead> ReadBodyAsync(
        HttpResponseMessage response,
        string? savePath,
        CancellationToken cancellationToken)
    {
        var contentType = response.Content.Headers.ContentType?.ToString();
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var preview = new MemoryStream();
        FileStream? save = null;
        var savedBytes = 0;
        var savedTruncated = false;
        string? saveError = null;
        try
        {
            if (savePath is not null)
            {
                var parent = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                save = new FileStream(
                    savePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 8192,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
            }

            var buffer = new byte[8192];
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                if (preview.Length < MaxResponseBodyBytes)
                {
                    var toPreview = (int)Math.Min(read, MaxResponseBodyBytes - preview.Length);
                    preview.Write(buffer, 0, toPreview);
                }

                if (save is null)
                {
                    if (preview.Length >= MaxResponseBodyBytes)
                    {
                        break;
                    }

                    continue;
                }

                if (savedBytes + read > MaxSaveBytes)
                {
                    var allowed = MaxSaveBytes - savedBytes;
                    if (allowed > 0)
                    {
                        await save.WriteAsync(buffer.AsMemory(0, allowed), cancellationToken);
                        savedBytes += allowed;
                    }

                    savedTruncated = true;
                    break;
                }

                await save.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                savedBytes += read;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            saveError = $"HTTP succeeded but saving '{savePath}' failed: {ex.Message}";
        }
        finally
        {
            if (save is not null)
            {
                await save.DisposeAsync();
            }
        }

        if (saveError is not null && savePath is not null)
        {
            TryDelete(savePath);
        }

        var previewBytes = preview.ToArray();
        if (previewBytes.Length == 0)
        {
            return new HttpBodyRead(
                Body: null,
                Truncated: false,
                Binary: false,
                ContentType: contentType,
                SavedBytes: savePath is null || saveError is not null ? null : savedBytes,
                SavedTruncated: savedTruncated,
                SaveError: saveError);
        }

        var binary = IsBinary(contentType, previewBytes);
        var inlineTruncated = !binary && previewBytes.Length >= MaxResponseBodyBytes;
        return new HttpBodyRead(
            Body: binary ? null : Encoding.UTF8.GetString(previewBytes),
            Truncated: inlineTruncated,
            Binary: binary,
            ContentType: contentType,
            SavedBytes: savePath is null || saveError is not null ? null : savedBytes,
            SavedTruncated: savedTruncated,
            SaveError: saveError);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static bool IsBinary(string? contentType, byte[] bytes)
    {
        if (contentType is not null)
        {
            var media = contentType.Split(';', 2)[0].Trim();
            if (media.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                || media.Contains("json", StringComparison.OrdinalIgnoreCase)
                || media.Contains("xml", StringComparison.OrdinalIgnoreCase)
                || media.Contains("javascript", StringComparison.OrdinalIgnoreCase)
                || media.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (media.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                || media.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
                || media.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                || media.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
                || media.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                || media.Equals("application/zip", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var inspect = Math.Min(bytes.Length, 512);
        return bytes.AsSpan(0, inspect).Contains((byte)0);
    }

    private static ImmutableDictionary<string, string> FlattenHeaders(HttpResponseMessage response)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            builder[header.Key] = string.Join(", ", header.Value);
        }

        foreach (var header in response.Content.Headers)
        {
            builder[header.Key] = string.Join(", ", header.Value);
        }

        return builder.ToImmutable();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' HTTP {Method} {Host} completed with {Status} (truncated={Truncated}, binary={Binary}).")]
    private partial void LogCompleted(string agentId, string method, string host, int status, bool truncated, bool binary);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' HTTP {Method} {Host} timed out after {TimeoutSeconds}s.")]
    private partial void LogTimedOut(string agentId, string method, string host, int timeoutSeconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' HTTP {Method} {Host} failed.")]
    private partial void LogFailed(string agentId, string method, string host, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' saved HTTP body to '{Path}' ({Bytes} bytes, truncated={Truncated}).")]
    private partial void LogSaved(string agentId, string path, int bytes, bool truncated);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' failed to save HTTP body to '{Path}': {Message}")]
    private partial void LogSaveFailed(string agentId, string path, string message);
}
