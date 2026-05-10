using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LlamaShears.Provider.OpenAI;

public sealed partial class OpenAiProviderFactory : IProviderFactory
{
    public const string ProviderName = "openai";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<OpenAiProviderOptions> _hostOptions;
    private readonly IServiceProvider _serviceProvider;

    public OpenAiProviderFactory(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<OpenAiProviderOptions> hostOptions,
        IServiceProvider serviceProvider)
    {
        _httpClientFactory = httpClientFactory;
        _hostOptions = hostOptions;
        _serviceProvider = serviceProvider;
    }

    public string Name => ProviderName;

    public async IAsyncEnumerable<ModelInfo> ListModelsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var options = _hostOptions.CurrentValue;
        var requestUri = new Uri(options.BaseUri, "v1/models");
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        if (!string.IsNullOrEmpty(options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }
        var httpClient = _httpClientFactory.CreateClient(nameof(OpenAiLanguageModel));
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var root = JsonNode.Parse(body) as JsonObject;
        if (root?["data"] is not JsonArray entries)
        {
            yield break;
        }

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry is not JsonObject obj)
            {
                continue;
            }
            var id = obj["id"]?.GetValue<string?>();
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }
            yield return new ModelInfo(
                ModelId: id,
                DisplayName: id,
                Description: obj["owned_by"]?.GetValue<string?>(),
                SupportedInputs: SupportedInputType.Text,
                SupportsReasoning: false,
                MaxContextWindow: 0);
        }
    }

    public ILanguageModel CreateModel(ModelConfiguration configuration)
        => ActivatorUtilities.CreateInstance<OpenAiLanguageModel>(_serviceProvider, configuration);
}
