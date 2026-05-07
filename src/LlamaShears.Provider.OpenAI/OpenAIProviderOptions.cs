using System.Text.Json.Nodes;

namespace LlamaShears.Provider.OpenAI;

public class OpenAIProviderOptions
{
    public Uri BaseUri { get; set; } = new("http://localhost:8080");

    public string ApiKey { get; set; } = string.Empty;

    // Time-to-first-byte cap on the chat HTTP call. Once the server
    // starts streaming this no longer applies (we use
    // HttpCompletionOption.ResponseHeadersRead). Cold-start model
    // loads on slow storage can run past .NET's 100s default before
    // any token arrives.
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(300);

    // Free-form fields merged into every request body so vendor knobs
    // (llama-server's cache_prompt / slot_id / samplers / n_probs,
    // vLLM's guided_choice, etc.) round-trip without forking the
    // provider per backend. Per-agent overrides via the agent's
    // Options blob layer on top with the usual deep-merge semantics.
    public JsonObject ExtraRequestParams { get; set; } = [];
}
