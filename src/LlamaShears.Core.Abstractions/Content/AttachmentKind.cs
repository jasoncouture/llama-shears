using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Content;

[JsonConverter(typeof(JsonStringEnumConverter<AttachmentKind>))]
public enum AttachmentKind
{
    Image,
}
