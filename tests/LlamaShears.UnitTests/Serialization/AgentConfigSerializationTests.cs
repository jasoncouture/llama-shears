using System.Text.Json;
using LlamaShears.Agent.Core;
using LlamaShears.Provider.Abstractions;

namespace LlamaShears.UnitTests.Serialization;

public sealed class AgentConfigSerializationTests
{
    // Match AgentManager's deserializer settings exactly so regressions
    // here mirror what the host actually does at runtime.
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    [Test]
    public async Task Regression_think_accepts_string_value_in_agent_json()
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
              "heartbeatPeriod": "00:00:30",
              "systemPrompt": "You are a helpful assistant. Keep replies short.",
              "seedTurn": "Tell me a one-sentence joke."
            }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config).IsNotNull();
        await Assert.That(config!.Model.Think).IsEqualTo(ThinkLevel.High);
    }

    [Test]
    public async Task Model_think_is_case_insensitive()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x", "think": "low" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.Think).IsEqualTo(ThinkLevel.Low);
    }

    [Test]
    public async Task Model_think_defaults_to_None_when_absent()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.Think).IsEqualTo(ThinkLevel.None);
    }

    [Test]
    public async Task Model_id_deserializes_via_ModelIdentity_converter()
    {
        const string json = """
            { "model": { "id": "OLLAMA/owner/repo:tag" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.Id.Provider).IsEqualTo("OLLAMA");
        await Assert.That(config.Model.Id.Model).IsEqualTo("owner/repo:tag");
    }

    [Test]
    public async Task Model_contextLength_round_trips_as_integer()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x", "contextLength": 262144 } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.ContextLength).IsEqualTo(262144);
    }

    [Test]
    public async Task Model_contextLength_defaults_to_null_when_absent()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.ContextLength).IsNull();
    }

    [Test]
    public async Task HeartbeatPeriod_round_trips_as_TimeSpan_string()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x" }, "heartbeatPeriod": "00:01:30" }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.HeartbeatPeriod).IsEqualTo(TimeSpan.FromSeconds(90));
    }

    [Test]
    public async Task HeartbeatPeriod_defaults_to_30_minutes_when_absent()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.HeartbeatPeriod).IsEqualTo(TimeSpan.FromMinutes(30));
    }

    [Test]
    public async Task Missing_required_model_throws()
    {
        const string json = """
            { "heartbeatPeriod": "00:00:30" }
            """;

        await Assert.That(() => JsonSerializer.Deserialize<AgentConfig>(json, _options))
            .Throws<JsonException>();
    }

    [Test]
    public async Task Missing_required_model_id_throws()
    {
        const string json = """
            { "model": { "think": "High" } }
            """;

        await Assert.That(() => JsonSerializer.Deserialize<AgentConfig>(json, _options))
            .Throws<JsonException>();
    }

    [Test]
    public async Task Invalid_think_string_throws()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x", "think": "Extreme" } }
            """;

        await Assert.That(() => JsonSerializer.Deserialize<AgentConfig>(json, _options))
            .Throws<JsonException>();
    }

    [Test]
    public async Task Model_keepAlive_defaults_to_null_when_absent()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.KeepAlive).IsNull();
    }

    [Test]
    public async Task Model_keepAlive_round_trips_as_TimeSpan_string()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x", "keepAlive": "01:00:00" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.KeepAlive).IsEqualTo(TimeSpan.FromHours(1));
    }

    [Test]
    public async Task Model_keepAlive_zero_means_unload_immediately()
    {
        const string json = """
            { "model": { "id": "OLLAMA/x", "keepAlive": "00:00:00" } }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.KeepAlive).IsEqualTo(TimeSpan.Zero);
    }

    [Test]
    public async Task Model_keepAlive_negative_means_never_unload()
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
