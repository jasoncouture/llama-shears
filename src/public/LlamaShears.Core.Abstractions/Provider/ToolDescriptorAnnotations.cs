namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Behavior hints attached to a <see cref="ToolDescriptor"/>. Mirrors the
/// optional MCP <c>ToolAnnotations</c> block (<c>destructiveHint</c> /
/// <c>idempotentHint</c> / <c>openWorldHint</c> / <c>readOnlyHint</c>) so
/// host code, UI surfaces, and confirmation gates can reason about a tool's
/// side-effect profile without re-deriving it from the schema. Values default
/// to the MCP-spec defaults so an absent annotation block degrades to the
/// conservative interpretation.
/// </summary>
/// <param name="Title">Human-friendly title for the tool. Sources should populate this from the MCP <c>title</c> annotation when present, otherwise fall back to the tool's own <c>title</c> or its <c>name</c>.</param>
/// <param name="Destructive">Indicates the tool might perform destructive updates to its environment. Defaults to <see langword="true"/> (per MCP spec); only relevant when <paramref name="ReadOnly"/> is <see langword="false"/>.</param>
/// <param name="Idempotent">Indicates that repeated invocations with identical arguments have no additional effect. Defaults to <see langword="false"/>; only relevant when <paramref name="ReadOnly"/> is <see langword="false"/>.</param>
/// <param name="OpenWorld">Indicates the tool can interact with an unpredictable / dynamic set of external entities (e.g. a web search or shell). Defaults to <see langword="true"/>; set to <see langword="false"/> for tools whose domain is closed and well-defined (e.g. workspace files, memory store).</param>
/// <param name="ReadOnly">Indicates the tool performs only reads and never modifies its environment. Defaults to <see langword="false"/>.</param>
public sealed record ToolDescriptorAnnotations(string Title, bool Destructive = true, bool Idempotent = false, bool OpenWorld = true, bool ReadOnly = false);
