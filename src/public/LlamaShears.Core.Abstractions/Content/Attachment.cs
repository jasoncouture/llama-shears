namespace LlamaShears.Core.Abstractions.Content;

/// <summary>
/// A non-text payload attached to a turn. Carried as base64 for
/// in-flight delivery to the language model. Image attachments are
/// not written to the context store and are stripped from live
/// context after the model has seen them.
/// </summary>
/// <param name="Kind">Modality of the attachment.</param>
/// <param name="MimeType">MIME type of the payload (<c>image/png</c>, <c>image/jpeg</c>, …).</param>
/// <param name="Base64Data">Payload encoded as base64.</param>
public sealed record Attachment(
    AttachmentKind Kind,
    string MimeType,
    string Base64Data);
