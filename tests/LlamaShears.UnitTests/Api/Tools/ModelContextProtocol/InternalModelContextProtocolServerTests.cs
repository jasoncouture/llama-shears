using LlamaShears.Api.Tools.ModelContextProtocol;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol;

public sealed class InternalModelContextProtocolServerTests
{
    [Test]
    [Arguments("http://[::]:8080", "http://localhost:8080/mcp")]
    [Arguments("http://0.0.0.0:8080", "http://localhost:8080/mcp")]
    [Arguments("http://192.168.1.10:5000", "http://localhost:5000/mcp")]
    [Arguments("https://[::]:8443", "https://localhost:8443/mcp")]
    public async Task UriRewritesHostToLocalhostAndKeepsPortAndScheme(string listenAddress, string expected)
    {
        var server = BuildServer(listenAddress);

        var subject = new InternalModelContextProtocolServer(server);

        await Assert.That(subject.Uri?.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task UriIsNullWhenNoAddressesAreBound()
    {
        var server = BuildServer();

        var subject = new InternalModelContextProtocolServer(server);

        await Assert.That(subject.Uri).IsNull();
    }

    [Test]
    public async Task UriPicksTheFirstAddressWhenMultipleAreBound()
    {
        var server = BuildServer("http://[::]:8080", "https://[::]:8443");

        var subject = new InternalModelContextProtocolServer(server);

        await Assert.That(subject.Uri?.ToString()).IsEqualTo("http://localhost:8080/mcp");
    }

    private static IServer BuildServer(params string[] addresses)
    {
        var addressesFeature = Substitute.For<IServerAddressesFeature>();
        addressesFeature.Addresses.Returns(addresses);

        var features = Substitute.For<IFeatureCollection>();
        features.Get<IServerAddressesFeature>().Returns(addressesFeature);

        var server = Substitute.For<IServer>();
        server.Features.Returns(features);
        return server;
    }
}
