namespace Narratoria.Components;

public sealed record AttachmentCandidate(
    string ClientId,
    string FileName,
    string MimeType,
    long SizeBytes,
    Func<CancellationToken, Stream> OpenReadStream);
