namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// One input accepted by a <see cref="ToolDescriptor"/>. Type is a plain
/// string (matches JSON-schema vocabulary: "string", "integer", "number",
/// "boolean", "object", "array") so templates can render it directly
/// without a switch.
/// </summary>
/// <param name="Name">Parameter name as the model writes it in a call.</param>
/// <param name="Description">Human-readable description shown to the model.</param>
/// <param name="Type">JSON-schema-style type token.</param>
/// <param name="Required">Whether the model must supply this parameter.</param>
public sealed record ToolParameter(
    string Name,
    string Description,
    string Type,
    bool Required);
