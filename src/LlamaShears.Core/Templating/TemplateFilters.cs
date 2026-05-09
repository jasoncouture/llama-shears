using Scriban.Runtime;

namespace LlamaShears.Core.Templating;

public static class TemplateFilters
{
    public static void Register(ScriptObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.Import("format_datetimeoffset", FormatDateTimeOffset);
    }

    private static string FormatDateTimeOffset(DateTimeOffset? value, string format)
        => value.HasValue ? value.Value.ToString(format) : string.Empty;
}
