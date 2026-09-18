using LlamaShears.Api.Web.Services;

namespace LlamaShears.UnitTests.Services;

public sealed class DataContextValueFormatterTests
{
    [Test]
    public async Task FormatNullReturnsNullLiteral()
    {
        await Assert.That(DataContextValueFormatter.Format(null)).IsEqualTo("null");
    }

    [Test]
    public async Task FormatPrimitiveSerializesAsJson()
    {
        await Assert.That(DataContextValueFormatter.Format(42)).IsEqualTo("42");
    }

    [Test]
    public async Task FormatRecordPrettyPrintsJson()
    {
        var formatted = DataContextValueFormatter.Format(new SamplePayload("alpha", 3));

        await Assert.That(formatted).Contains("\"Name\": \"alpha\"");
        await Assert.That(formatted).Contains("\"Count\": 3");
        await Assert.That(formatted).Contains('\n');
    }

    [Test]
    public async Task FormatUnserializableValueFallsBackToToString()
    {
        var value = (Func<int>)(() => 1);

        await Assert.That(DataContextValueFormatter.Format(value)).IsEqualTo(value.ToString());
    }

    private sealed record SamplePayload(string Name, int Count);
}
