using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Content;

/// <summary>
/// Modality of a non-text payload attached to a turn. Today the
/// framework recognizes only <see cref="Image"/>; additional kinds
/// will be added as providers gain support.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AttachmentKind>))]
public enum AttachmentKind
{
    /// <summary>An image attachment (PNG, JPEG, etc.).</summary>
    Image,
}
