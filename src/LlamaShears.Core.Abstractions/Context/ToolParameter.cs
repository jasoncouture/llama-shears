namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// One parameter in a <see cref="ToolDescriptor"/>'s schema.
/// </summary>
/// <param name="Name">Parameter name.</param>
/// <param name="Description">Human-readable description shown to the model.</param>
/// <param name="Type">JSON-schema-flavored type tag (e.g. <c>"string"</c>, <c>"integer"</c>).</param>
/// <param name="Required">Whether the parameter must be supplied for the call to be valid.</param>
public sealed record ToolParameter(
    string Name,
    string Description,
    string Type,
    bool Required);
