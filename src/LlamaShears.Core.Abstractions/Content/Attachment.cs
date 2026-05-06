namespace LlamaShears.Core.Abstractions.Content;

/// <summary>
/// A non-text payload attached to a turn. Carried as base64 so the
/// turn can be persisted and replayed verbatim without separate blob
/// storage.
/// </summary>
/// <param name="Kind">Modality of the attachment.</param>
/// <param name="MimeType">MIME type of the payload (<c>image/png</c>, <c>image/jpeg</c>, …).</param>
/// <param name="Base64Data">Payload encoded as base64.</param>
public sealed record Attachment(
    AttachmentKind Kind,
    string MimeType,
    string Base64Data);
