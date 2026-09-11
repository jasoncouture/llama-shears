using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Http;

public static class HttpRequestServiceCollectionExtensions
{
    public static IServiceCollection AddHttpRequestTools(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient(HttpTools.HttpClientName, static client =>
            {
                client.Timeout = TimeSpan.FromSeconds(HttpTools.MaxTimeoutSeconds);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("LlamaShears");
            })
            .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);
        return services;
    }

    public static SocketsHttpHandler CreatePrimaryHandler()
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
            UseCookies = false,
        };
    }
}
