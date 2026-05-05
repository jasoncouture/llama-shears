namespace LlamaShears.Core.Abstractions.Content;

public sealed record Attachment(
    AttachmentKind Kind,
    string MimeType,
    string Base64Data);
