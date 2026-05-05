using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol;

[McpServerToolType]
public sealed class RandomNumberTool
{
    [McpServerTool(Name = "random_number")]
    [Description("Returns a uniformly random integer between min and max, inclusive on both ends. The agent has no way to predict the result without calling the tool.")]
    public string RandomNumber(
        [Description("Lower bound, inclusive.")] int min,
        [Description("Upper bound, inclusive.")] int max)
    {
        if (min > max)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Invalid range: min ({0}) is greater than max ({1}).",
                min,
                max);
        }
        // Random.Next's upper is exclusive; widen by one for inclusive
        // semantics, guarding the int.MaxValue overflow case.
        var exclusiveUpper = max == int.MaxValue ? max : max + 1;
        var value = Random.Shared.Next(min, exclusiveUpper);
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
