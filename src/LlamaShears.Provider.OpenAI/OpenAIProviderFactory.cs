using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LlamaShears.Provider.OpenAI;

public sealed class OpenAiProviderFactory : IProviderFactory
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
        OpenAiRequestHeaders.Apply(request, options.Headers);
        var httpClient = _httpClientFactory.CreateClient(nameof(OpenAiLanguageModel));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

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

    public async ValueTask<ValidationResult?> ValidateAsync(ModelConfiguration configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configuration.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration.Id.Model);

        if (!string.Equals(configuration.Id.Provider, Name, StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                $"Provider '{configuration.Id.Provider}' does not match this factory ('{Name}').",
                [nameof(ModelConfiguration.Id)]);
        }

        await foreach (var model in ListModelsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(model.ModelId, configuration.Id.Model, StringComparison.Ordinal))
            {
                return ValidationResult.Success;
            }
        }
        return new ValidationResult(
            $"OpenAI provider does not have a model named '{configuration.Id.Model}'.",
            [nameof(ModelConfiguration.Id)]);
    }

    public ILanguageModel CreateModel(ModelConfiguration configuration)
        => ActivatorUtilities.CreateInstance<OpenAiLanguageModel>(_serviceProvider, configuration);
}
