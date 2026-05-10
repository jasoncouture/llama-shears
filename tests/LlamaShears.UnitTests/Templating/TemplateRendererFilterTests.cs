using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Templating;
using NSubstitute;
using Scriban;

namespace LlamaShears.UnitTests.Templating;

public sealed class TemplateRendererFilterTests
{
    [Test]
    public async Task FormatDateTimeOffsetAppliesNetFormatStringWithOffset()
    {
        var renderer = BuildRendererWith("{{ now | format_datetimeoffset 'yyyy-MM-ddTHH:mm:sszzz' }}");
        var input = BuildInput(new DateTimeOffset(2026, 5, 9, 13, 59, 22, TimeSpan.FromHours(-4)));

        var result = await renderer.RenderAsync("ignored", input, CancellationToken.None);

        await Assert.That(result).IsEqualTo("2026-05-09T13:59:22-04:00");
    }

    [Test]
    public async Task FormatDateTimeOffsetAcceptsAnyNetFormatString()
    {
        var renderer = BuildRendererWith("{{ now | format_datetimeoffset 'yyyy-MM-dd HH:mm' }}");
        var input = BuildInput(new DateTimeOffset(2026, 5, 9, 13, 59, 22, TimeSpan.FromHours(2)));

        var result = await renderer.RenderAsync("ignored", input, CancellationToken.None);

        await Assert.That(result).IsEqualTo("2026-05-09 13:59");
    }

    [Test]
    public async Task FormatDateTimeOffsetReturnsEmptyForNullInput()
    {
        var renderer = BuildRendererWith("[{{ now | format_datetimeoffset 'yyyy-MM-dd' }}]");
        var input = BuildInput(null);

        var result = await renderer.RenderAsync("ignored", input, CancellationToken.None);

        await Assert.That(result).IsEqualTo("[]");
    }

    [Test]
    public async Task FormatDateTimeOffsetStripsSubSecondsWhenFormatOmitsThem()
    {
        var renderer = BuildRendererWith("{{ now | format_datetimeoffset 'yyyy-MM-ddTHH:mm:sszzz' }}");
        var stamp = new DateTime(2026, 5, 9, 13, 59, 22, DateTimeKind.Unspecified).AddTicks(1426858);
        var input = BuildInput(new DateTimeOffset(stamp, TimeSpan.FromHours(-4)));

        var result = await renderer.RenderAsync("ignored", input, CancellationToken.None);

        await Assert.That(result).IsEqualTo("2026-05-09T13:59:22-04:00");
    }

    [Test]
    public async Task FormatDateTimeOffsetPreservesSubSecondsWhenFormatIncludesThem()
    {
        var renderer = BuildRendererWith("{{ now | format_datetimeoffset 'yyyy-MM-ddTHH:mm:ss.fffffffzzz' }}");
        var stamp = new DateTime(2026, 5, 9, 13, 59, 22, DateTimeKind.Unspecified).AddTicks(1426858);
        var input = BuildInput(new DateTimeOffset(stamp, TimeSpan.FromHours(-4)));

        var result = await renderer.RenderAsync("ignored", input, CancellationToken.None);

        await Assert.That(result).IsEqualTo("2026-05-09T13:59:22.1426858-04:00");
    }

    private static IReadOnlyDictionary<string, object?> BuildInput(DateTimeOffset? now) =>
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["now"] = now,
        };

    private static TemplateRenderer BuildRendererWith(string templateSource)
    {
        var template = Template.Parse(templateSource);
        var cache = Substitute.For<IFileParserCache<TemplateRenderer>>();
        cache.GetOrParseAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Func<Stream?, string, CancellationToken, ValueTask<Template?>>>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Template?>(template));
        return new TemplateRenderer(cache);
    }

}
