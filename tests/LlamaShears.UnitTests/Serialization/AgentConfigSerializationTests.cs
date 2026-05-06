using System.Text.Json;
using LlamaShears.Agent.Core;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Serialization;

public sealed class AgentConfigSerializationTests
{
    // Match AgentManager's deserializer settings exactly so regressions
    // here mirror what the host actually does at runtime.
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    [Test]
    public async Task RegressionThinkAcceptsStringValueInAgentJson()
    {
        // Regression for the JsonException seen on agent load:
        //   "The JSON value could not be converted to ... ThinkLevel"
        // when the agent JSON had `"think": "High"`. STJ's default for
        // enums is the integer underlying value; humans write strings.
        const string json = """
            {
              "model": {
                "id": "OLLAMA/gemma4:26b",
                "think": "High"
              },
              "heartbeatPeriod": "00:00:30"
            }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config).IsNotNull();
        await Assert.That(config!.Model.Think).IsEqualTo(ThinkLevel.High);
    }

    [Test]
    public async Task ModelThinkIsCaseInsensitive()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x", "think": "low" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.Think).IsEqualTo(ThinkLevel.Low);
    }

    [Test]
    public async Task ModelThinkDefaultsToNoneWhenAbsent()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.Think).IsEqualTo(ThinkLevel.None);
    }

    [Test]
    public async Task ModelIdDeserializesViaModelIdentityConverter()
    {
        const string json = """
            { "model": { "id": "OLLAMA/owner/repo:tag" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.Id.Provider).IsEqualTo("OLLAMA");
        await Assert.That(config.Model.Id.Model).IsEqualTo("owner/repo:tag");
    }

    [Test]
    public async Task ModelContextLengthRoundTripsAsInteger()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x", "contextLength": 262144 } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.ContextLength).IsEqualTo(262144);
    }

    [Test]
    public async Task ModelContextLengthDefaultsToNullWhenAbsent()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.ContextLength).IsNull();
    }

    [Test]
    public async Task HeartbeatPeriodRoundTripsAsTimeSpanString()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x" }, "heartbeatPeriod": "00:01:30" }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.HeartbeatPeriod).IsEqualTo(TimeSpan.FromSeconds(90));
    }

    [Test]
    public async Task HeartbeatPeriodDefaultsTo30MinutesWhenAbsent()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.HeartbeatPeriod).IsEqualTo(TimeSpan.FromMinutes(30));
    }

    [Test]
    public async Task MissingRequiredModelThrows()
    {
        const string json = """
            { "heartbeatPeriod": "00:00:30" }
            """;

        await Assert.That(() => JsonSerializer.Deserialize<AgentConfig>(json, _options))
            .Throws<JsonException>();
    }

    [Test]
    public async Task MissingRequiredModelIdThrows()
    {
        const string json = """
            { "model": { "think": "High" } }
            """;

        await Assert.That(() => JsonSerializer.Deserialize<AgentConfig>(json, _options))
            .Throws<JsonException>();
    }

    [Test]
    public async Task InvalidThinkStringThrows()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x", "think": "Extreme" } }
            """;

        await Assert.That(() => JsonSerializer.Deserialize<AgentConfig>(json, _options))
            .Throws<JsonException>();
    }

    [Test]
    public async Task ModelKeepAliveDefaultsToNullWhenAbsent()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.KeepAlive).IsNull();
    }

    [Test]
    public async Task ModelKeepAliveRoundTripsAsTimeSpanString()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x", "keepAlive": "01:00:00" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.KeepAlive).IsEqualTo(TimeSpan.FromHours(1));
    }

    [Test]
    public async Task ModelKeepAliveZeroMeansUnloadImmediately()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x", "keepAlive": "00:00:00" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.KeepAlive).IsEqualTo(TimeSpan.Zero);
    }

    [Test]
    public async Task ModelKeepAliveNegativeMeansNeverUnload()
    {
        // Convention: any negative TimeSpan means "never unload." STJ
        // parses "-00:00:01" into a negative TimeSpan via the standard
        // invariant format; no custom converter needed.
        const string json = """
            { "model": { "id": "OLLAMA/x", "keepAlive": "-00:00:01" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.KeepAlive).IsNotNull();
        await Assert.That(config!.Model.KeepAlive < TimeSpan.Zero).IsTrue();
    }
}
