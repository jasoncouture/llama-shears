namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Caller-supplied configuration for an ephemeral session: which system
/// prompt template to render, any extra data the template needs, an
/// optional iteration cap, and an optional channel id for tagging events
/// and replies.
/// </summary>
/// <param name="SystemPromptTemplate">
/// Template file name to feed into <c>PromptOptions.SystemPromptTemplate</c>
/// for every inference inside this session (e.g. <c>SUBAGENT.md</c>). Required.
/// </param>
/// <param name="TemplateData">
/// Optional key/value pairs merged into the session's data scope so the
/// template can interpolate them. <see langword="null"/> = no extra data.
/// </param>
/// <param name="MaxIterations">
/// Maximum number of inference iterations the session loop will run
/// before forcing exit. <see langword="null"/> uses the implementation
/// default.
/// </param>
/// <param name="ChannelId">
/// Caller-chosen channel id used for tagging events emitted from inside
/// the session and for the <c>:&lt;channelId&gt;</c> segment on the
/// <c>session_reply</c> event. <see langword="null"/> = the
/// implementation falls back to a per-session synthesized id.
/// </param>
public sealed record EphemeralSessionRequest(
    string SystemPromptTemplate,
    IReadOnlyDictionary<string, object?>? TemplateData = null,
    int? MaxIterations = null,
    string? ChannelId = null);
