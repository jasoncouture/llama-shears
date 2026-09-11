using Microsoft.Extensions.Options;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Discord;

public sealed class DiscordWebhookOptionsValidator : IValidateOptions<DiscordWebhookOptions>
{
    public ValidateOptionsResult Validate(string? name, DiscordWebhookOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Webhooks is null)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        foreach (var (key, url) in options.Webhooks)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                failures.Add("Discord webhook names must be non-empty.");
                continue;
            }

            if (!DiscordWebhookUrl.TryValidate(url, out _, out var error))
            {
                failures.Add($"Discord webhook '{key}': {error}");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
