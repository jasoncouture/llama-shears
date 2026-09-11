using LlamaShears.Api.Tools.ModelContextProtocol.Http;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Http;

public sealed class HttpRequestServiceCollectionExtensionsTests
{
    [Test]
    public async Task PrimaryHandlerDisablesCookiesAndCapsRedirects()
    {
        using var handler = HttpRequestServiceCollectionExtensions.CreatePrimaryHandler();

        await Assert.That(handler.UseCookies).IsFalse();
        await Assert.That(handler.AllowAutoRedirect).IsTrue();
        await Assert.That(handler.MaxAutomaticRedirections).IsEqualTo(10);
    }
}
