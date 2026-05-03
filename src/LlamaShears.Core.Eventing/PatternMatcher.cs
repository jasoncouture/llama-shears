using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using LlamaShears.Core.Abstractions.Events;

namespace LlamaShears.Core.Eventing;

public sealed class PatternMatcher : IPatternMatcher
{
    private readonly ConcurrentDictionary<string, CompiledPattern> _cache = new(StringComparer.Ordinal);

    public bool IsMatch(string pattern, EventType type)
        => _cache.GetOrAdd(pattern, static raw => Compile(raw)).IsMatch(type.ToString());

    private static CompiledPattern Compile(string pattern)
    {
        var segments = pattern.Split(':');
        return HasWildcard(segments)
            ? new RegexPattern(BuildRegex(segments))
            : new DirectMatchPattern(pattern);
    }

    private static bool HasWildcard(string[] segments)
    {
        for (var index = 0; index < segments.Length; index++)
        {
            if (segments[index] == "*" || segments[index] == "+")
            {
                return true;
            }
        }
        return false;
    }

    private static Regex BuildRegex(string[] segments)
    {
        var builder = new StringBuilder("^");
        for (var index = 0; index < segments.Length; index++)
        {
            AppendSegment(builder, segments[index], leadingColon: index > 0);
        }
        builder.Append('$');
        return new Regex(builder.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private static void AppendSegment(StringBuilder builder, string segment, bool leadingColon)
    {
        switch (segment)
        {
            case "*":
                builder.Append(leadingColon ? "(?::[^:]+)*" : "(?:[^:]+(?::[^:]+)*)?");
                break;
            case "+":
                builder.Append(leadingColon ? "(?::[^:]+)+" : "[^:]+(?::[^:]+)*");
                break;
            default:
                if (leadingColon)
                {
                    builder.Append(':');
                }
                builder.Append(Regex.Escape(segment));
                break;
        }
    }

    private abstract class CompiledPattern
    {
        public abstract bool IsMatch(string value);
    }

    private sealed class DirectMatchPattern : CompiledPattern
    {
        private readonly string _pattern;

        public DirectMatchPattern(string pattern)
        {
            _pattern = pattern;
        }

        public override bool IsMatch(string value)
            => string.Equals(_pattern, value, StringComparison.Ordinal);
    }

    private sealed class RegexPattern : CompiledPattern
    {
        private readonly Regex _regex;

        public RegexPattern(Regex regex)
        {
            _regex = regex;
        }

        public override bool IsMatch(string value)
            => _regex.IsMatch(value);
    }
}
