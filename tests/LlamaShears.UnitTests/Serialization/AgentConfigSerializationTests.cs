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
              "model": "OLLAMA/gemma4:26b",
              "heartbeatPeriod": "00:00:30",
              "systemPrompt": "You are a helpful assistant. Keep replies short.",
              "seedTurn": "Tell me a one-sentence joke.",
              "think": "High"
            }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config).IsNotNull();
        await Assert.That(config!.Think).IsEqualTo(ThinkLevel.High);
    }

    [Test]
    public async Task Think_is_case_insensitive()
    {
        const string json = """
            { "model": "OLLAMA/x", "think": "low" }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Think).IsEqualTo(ThinkLevel.Low);
    }

    [Test]
    public async Task Think_defaults_to_None_when_absent()
    {
        const string json = """
            { "model": "OLLAMA/x" }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Think).IsEqualTo(ThinkLevel.None);
    }

    [Test]
    public async Task Model_deserializes_via_ModelIdentity_converter()
    {
        const string json = """
            { "model": "OLLAMA/owner/repo:tag" }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.Model.Provider).IsEqualTo("OLLAMA");
        await Assert.That(config.Model.Model).IsEqualTo("owner/repo:tag");
    }

    [Test]
    public async Task HeartbeatPeriod_round_trips_as_TimeSpan_string()
    {
        const string json = """
            { "model": "OLLAMA/x", "heartbeatPeriod": "00:01:30" }
            """;

        var config = JsonSerializer.Deserialize<AgentConfig>(json, _options);

        await Assert.That(config!.HeartbeatPeriod).IsEqualTo(TimeSpan.FromSeconds(90));
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
    public async Task Invalid_think_string_throws()
    {
        const string json = """
            { "model": "OLLAMA/x", "think": "Extreme" }
            """;

        await Assert.That(() => JsonSerializer.Deserialize<AgentConfig>(json, _options))
            .Throws<JsonException>();
    }
}
