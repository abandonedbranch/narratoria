using System.Collections.Immutable;
using Narratoria.OpenAi;
using Narratoria.Storage;

namespace Narratoria.Narration.Attachments;

public sealed record UploadedFile(
    Guid SessionId,
    string AttachmentId,
    string FileName,
    string MimeType,
    long SizeBytes,
    byte[] Content);

public sealed record AttachmentIngestionOptions
{
    public static readonly AttachmentIngestionOptions Default = new();

    public long MaxBytes { get; init; } = 4 * 1024 * 1024;

    public int? MaxChars { get; init; }

    public int? MaxTokens { get; init; }

    public bool DedupeByHash { get; init; } = true;

    public IImmutableSet<string> AllowedMimeTypes { get; init; } =
        ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase, "text/plain", "text/markdown");
}

public sealed record ProcessedAttachment(
    string AttachmentId,
    Guid SessionId,
    string FileName,
    string MimeType,
    string SourceHash,
    string NormalizedText,
    int TokenEstimate,
    bool Truncated,
    DateTimeOffset CreatedAt);

public sealed record AttachmentIngestionCommand(
    Guid SessionId,
    string AttachmentId,
    TraceMetadata Trace,
    AttachmentIngestionOptions Options);

public sealed record AttachmentIngestionError(string ErrorClass, string Message);

public readonly record struct AttachmentIngestionResult(bool Ok, ProcessedAttachment? Attachment, AttachmentIngestionError? Error)
{
    public static AttachmentIngestionResult Success(ProcessedAttachment attachment) => new(true, attachment, null);

    public static AttachmentIngestionResult Failure(AttachmentIngestionError error) => new(false, null, error);
}

public interface IAttachmentUploadStore
{
    ValueTask<string> WriteAsync(Guid sessionId, string fileName, string mimeType, long sizeBytes, Stream content, CancellationToken cancellationToken);

    ValueTask<UploadedFile?> GetAsync(Guid sessionId, string attachmentId, CancellationToken cancellationToken);

    ValueTask DeleteAsync(Guid sessionId, string attachmentId, CancellationToken cancellationToken);
}

public interface IProcessedAttachmentStore
{
    ValueTask<StorageResult<ProcessedAttachment?>> GetAsync(Guid sessionId, string attachmentId, CancellationToken cancellationToken);

    ValueTask<StorageResult<IReadOnlyList<ProcessedAttachment>>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken);

    ValueTask<StorageResult<ProcessedAttachment?>> FindByHashAsync(Guid sessionId, string sourceHash, CancellationToken cancellationToken);

    ValueTask<StorageResult<Unit>> SaveAsync(ProcessedAttachment attachment, CancellationToken cancellationToken);

    ValueTask<StorageResult<Unit>> DeleteAsync(Guid sessionId, string attachmentId, CancellationToken cancellationToken);
}

public interface IAttachmentOpenAiContextFactory
{
    OpenAiRequestContext Create(TraceMetadata trace);
}

public interface IAttachmentIngestionService
{
    ValueTask<AttachmentIngestionResult> IngestAsync(AttachmentIngestionCommand command, CancellationToken cancellationToken);
}

public interface IAttachmentIngestionMetrics
{
    void RecordProcessed(string status, string errorClass);

    void RecordProviderLatency(TimeSpan duration);

    void RecordPersistenceLatency(TimeSpan duration);

    void RecordBytesIngested(long bytes);
}

public sealed class NullAttachmentIngestionMetrics : IAttachmentIngestionMetrics
{
    public static readonly NullAttachmentIngestionMetrics Instance = new();

    public void RecordProcessed(string status, string errorClass)
    {
    }

    public void RecordProviderLatency(TimeSpan duration)
    {
    }

    public void RecordPersistenceLatency(TimeSpan duration)
    {
    }

    public void RecordBytesIngested(long bytes)
    {
    }
}
