using System.Text.Json;
using Narratoria.Storage;
using Narratoria.Storage.IndexedDb;

namespace Narratoria.Narration.Attachments;

public sealed class UploadedFileSerializer : IIndexedDbValueSerializer<UploadedFile>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ValueTask<IndexedDbSerializedValue> SerializeAsync(UploadedFile value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, Options);
        return ValueTask.FromResult(new IndexedDbSerializedValue(payload, payload.LongLength));
    }

    public ValueTask<UploadedFile> DeserializeAsync(IndexedDbSerializedValue payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var upload = JsonSerializer.Deserialize<UploadedFile>(payload.Payload, Options)
            ?? throw new InvalidOperationException("Unable to deserialize uploaded file.");
        return ValueTask.FromResult(upload);
    }
}

public sealed class AttachmentUploadStore : IAttachmentUploadStore
{
    private readonly IIndexedDbStorageService _storage;
    private readonly IIndexedDbStorageWithQuota _quotaStorage;
    private readonly IIndexedDbValueSerializer<UploadedFile> _serializer;
    private readonly IndexedDbStoreDefinition _store;
    private readonly StorageScope _scope;
    private readonly ILogger<AttachmentUploadStore> _logger;

    public AttachmentUploadStore(
        IIndexedDbStorageService storage,
        IIndexedDbStorageWithQuota quotaStorage,
        IndexedDbStoreDefinition store,
        StorageScope scope,
        ILogger<AttachmentUploadStore> logger,
        IIndexedDbValueSerializer<UploadedFile>? serializer = null)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _quotaStorage = quotaStorage ?? throw new ArgumentNullException(nameof(quotaStorage));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _scope = scope;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? new UploadedFileSerializer();
    }

    public async ValueTask<string> WriteAsync(Guid sessionId, string fileName, string mimeType, long sizeBytes, Stream content, CancellationToken cancellationToken)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("SessionId is required.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("FileName is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(mimeType)) throw new ArgumentException("MimeType is required.", nameof(mimeType));
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        var attachmentId = Guid.NewGuid().ToString("N");

        byte[] bytes;
        await using (content.ConfigureAwait(false))
        {
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            bytes = ms.ToArray();
        }

        var upload = new UploadedFile(sessionId, attachmentId, fileName, mimeType, sizeBytes, bytes);

        var request = new IndexedDbPutRequest<UploadedFile>
        {
            Store = _store,
            Key = upload.AttachmentId,
            Value = upload,
            Serializer = _serializer,
            Scope = _scope,
            IndexValues = new Dictionary<string, object?>
            {
                ["session_id"] = sessionId.ToString(),
                ["mime_type"] = mimeType
            }
        };

        var result = await _quotaStorage.PutIfCanAccommodateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
        {
            throw new InvalidOperationException(result.Error?.Message ?? "Unable to write uploaded attachment.");
        }

        _logger.LogInformation("Attachment upload stored session={SessionId} attachment={AttachmentId} mime={MimeType} size={SizeBytes}", sessionId, attachmentId, mimeType, sizeBytes);
        return attachmentId;
    }

    public async ValueTask<UploadedFile?> GetAsync(Guid sessionId, string attachmentId, CancellationToken cancellationToken)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("SessionId is required.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(attachmentId)) throw new ArgumentException("AttachmentId is required.", nameof(attachmentId));

        var request = new IndexedDbGetRequest<UploadedFile>
        {
            Store = _store,
            Key = attachmentId,
            Serializer = _serializer,
            Scope = _scope
        };

        var result = await _storage.GetAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
        {
            _logger.LogWarning("Attachment upload get failed session={SessionId} attachment={AttachmentId} errorClass={ErrorClass} message={Message}", sessionId, attachmentId, result.Error?.ErrorClass, result.Error?.Message);
            return null;
        }

        var upload = result.Value;
        if (upload is null) return null;
        if (upload.SessionId != sessionId) return null;
        return upload;
    }

    public async ValueTask DeleteAsync(Guid sessionId, string attachmentId, CancellationToken cancellationToken)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("SessionId is required.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(attachmentId)) throw new ArgumentException("AttachmentId is required.", nameof(attachmentId));

        var existing = await GetAsync(sessionId, attachmentId, cancellationToken).ConfigureAwait(false);
        if (existing is null) return;

        var request = new IndexedDbDeleteRequest
        {
            Store = _store,
            Key = attachmentId,
            Scope = _scope
        };

        var result = await _storage.DeleteAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
        {
            _logger.LogWarning("Attachment upload delete failed session={SessionId} attachment={AttachmentId} errorClass={ErrorClass} message={Message}", sessionId, attachmentId, result.Error?.ErrorClass, result.Error?.Message);
        }
    }

    public static IndexedDbStoreDefinition CreateStoreDefinition(string name = "attachment_uploads")
    {
        return new IndexedDbStoreDefinition
        {
            Name = name,
            KeyPath = "AttachmentId",
            AutoIncrement = false,
            Indexes = new[]
            {
                new IndexedDbIndexDefinition { Name = "session_id", KeyPath = "SessionId", Unique = false, MultiEntry = false },
                new IndexedDbIndexDefinition { Name = "mime_type", KeyPath = "MimeType", Unique = false, MultiEntry = false }
            }
        };
    }
}
