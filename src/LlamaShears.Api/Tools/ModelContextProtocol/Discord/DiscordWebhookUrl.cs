using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Discord;

public static partial class DiscordWebhookUrl
{
    public static bool TryValidate(
        string? value,
        [NotNullWhen(true)] out Uri? uri,
        [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            uri = null;
            error = "Webhook URL is required.";
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            uri = null;
            error = "Webhook URL must be an https URI.";
            return false;
        }

        if (!IsAllowedHost(uri.Host))
        {
            uri = null;
            error = "Webhook URL must target discord.com or discordapp.com.";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            uri = null;
            error = "Webhook URL must not include a query string or fragment.";
            return false;
        }

        if (!PathPattern().IsMatch(uri.AbsolutePath))
        {
            uri = null;
            error = "Webhook URL path must be /api/webhooks/{id}/{token}.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsAllowedHost(string host)
    {
        return host.Equals("discord.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("discordapp.com", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^/api(?:/v\d+)?/webhooks/\d+/[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
    private static partial Regex PathPattern();
}
