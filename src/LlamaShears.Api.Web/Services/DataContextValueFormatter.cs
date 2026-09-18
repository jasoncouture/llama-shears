using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlamaShears.Api.Web.Services;

public static class DataContextValueFormatter
{
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Format(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        try
        {
            return JsonSerializer.Serialize(value, value.GetType(), _options);
        }
        catch (Exception)
        {
            return value.ToString() ?? "null";
        }
    }
}
