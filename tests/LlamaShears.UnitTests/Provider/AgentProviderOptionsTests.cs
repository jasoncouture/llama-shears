using System.Text.Json;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Provider;

public sealed class AgentProviderOptionsTests
{
    public sealed class Sample
    {
        public string Name { get; set; } = "host";
        public int Port { get; set; } = 80;
        public Nested Inner { get; set; } = new();
        public string[] Tags { get; set; } = ["a", "b"];
    }

    public sealed class Nested
    {
        public string A { get; set; } = "host-a";
        public string B { get; set; } = "host-b";
    }

    [Test]
    public async Task NullOverrideReturnsHostInstanceUnchanged()
    {
        var host = new Sample { Name = "fixed", Port = 1234 };

        var merged = AgentProviderOptions.Resolve<Sample>(host, agentOverride: null);

        await Assert.That(merged).IsSameReferenceAs(host);
    }

    [Test]
    public async Task ScalarOverrideReplacesHostScalar()
    {
        var host = new Sample { Name = "host", Port = 80 };
        var overlay = JsonDocument.Parse("""{"Name":"agent"}""").RootElement;

        var merged = AgentProviderOptions.Resolve<Sample>(host, overlay);

        await Assert.That(merged.Name).IsEqualTo("agent");
        await Assert.That(merged.Port).IsEqualTo(80);
    }

    [Test]
    public async Task NestedObjectMergesFieldByField()
    {
        var host = new Sample();
        var overlay = JsonDocument.Parse("""{"Inner":{"A":"agent-a"}}""").RootElement;

        var merged = AgentProviderOptions.Resolve<Sample>(host, overlay);

        await Assert.That(merged.Inner.A).IsEqualTo("agent-a");
        await Assert.That(merged.Inner.B).IsEqualTo("host-b");
    }

    [Test]
    public async Task ArrayOverrideReplacesEntireArray()
    {
        var host = new Sample();
        var overlay = JsonDocument.Parse("""{"Tags":["x"]}""").RootElement;

        var merged = AgentProviderOptions.Resolve<Sample>(host, overlay);

        await Assert.That(merged.Tags.Length).IsEqualTo(1);
        await Assert.That(merged.Tags[0]).IsEqualTo("x");
    }

    [Test]
    public async Task EmptyOverlayLeavesHostValuesIntact()
    {
        var host = new Sample { Name = "host", Port = 99 };
        var overlay = JsonDocument.Parse("{}").RootElement;

        var merged = AgentProviderOptions.Resolve<Sample>(host, overlay);

        await Assert.That(merged.Name).IsEqualTo("host");
        await Assert.That(merged.Port).IsEqualTo(99);
    }

    [Test]
    public async Task DoesNotMutateHostInstance()
    {
        var host = new Sample { Name = "host" };
        var overlay = JsonDocument.Parse("""{"Name":"agent"}""").RootElement;

        _ = AgentProviderOptions.Resolve<Sample>(host, overlay);

        await Assert.That(host.Name).IsEqualTo("host");
    }
}
