using System.Text.Json;
using System.Linq;
using Narratoria.Storage;
using Narratoria.Storage.IndexedDb;

namespace Narratoria.Narration.Attachments;

public sealed class ProcessedAttachmentSerializer : IIndexedDbValueSerializer<ProcessedAttachment>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ValueTask<IndexedDbSerializedValue> SerializeAsync(ProcessedAttachment value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, Options);
        return ValueTask.FromResult(new IndexedDbSerializedValue(payload, payload.LongLength));
    }

    public ValueTask<ProcessedAttachment> DeserializeAsync(IndexedDbSerializedValue payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var attachment = JsonSerializer.Deserialize<ProcessedAttachment>(payload.Payload, Options)
            ?? throw new InvalidOperationException("Unable to deserialize processed attachment.");
        return ValueTask.FromResult(attachment);
    }
}

public sealed class ProcessedAttachmentStore : IProcessedAttachmentStore
{
    private readonly IIndexedDbStorageService _storage;
    private readonly IIndexedDbStorageWithQuota _quotaStorage;
    private readonly IIndexedDbValueSerializer<ProcessedAttachment> _serializer;
    private readonly IndexedDbStoreDefinition _store;
    private readonly StorageScope _scope;
    private readonly ILogger<ProcessedAttachmentStore> _logger;

    public ProcessedAttachmentStore(
        IIndexedDbStorageService storage,
        IIndexedDbStorageWithQuota quotaStorage,
        IndexedDbStoreDefinition store,
        StorageScope scope,
        ILogger<ProcessedAttachmentStore> logger,
        IIndexedDbValueSerializer<ProcessedAttachment>? serializer = null)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _quotaStorage = quotaStorage ?? throw new ArgumentNullException(nameof(quotaStorage));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _scope = scope;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? new ProcessedAttachmentSerializer();
    }

    public async ValueTask<ProcessedAttachment?> FindByHashAsync(Guid sessionId, string sourceHash, CancellationToken cancellationToken)
    {
        var query = new IndexedDbQueryOptions("source_hash", sourceHash, null);
        var request = new IndexedDbListRequest<ProcessedAttachment>
        {
            Store = _store,
            Serializer = _serializer,
            Query = query,
            Scope = _scope
        };

        var result = await _storage.ListAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
        {
            _logger.LogWarning(
                "Processed attachment lookup failed session={SessionId} errorClass={ErrorClass} message={Message}",
                sessionId,
                result.Error?.ErrorClass,
                result.Error?.Message);
            return null;
        }

        var records = result.Value ?? Array.Empty<IndexedDbRecord<ProcessedAttachment>>();
        var match = records.FirstOrDefault(a => a.Value.SessionId == sessionId);
        return match.Equals(default) ? null : match.Value;
    }

    public ValueTask<StorageResult<Unit>> SaveAsync(ProcessedAttachment attachment, CancellationToken cancellationToken)
    {
        var request = new IndexedDbPutRequest<ProcessedAttachment>
        {
            Store = _store,
            Key = attachment.AttachmentId,
            Value = attachment,
            Serializer = _serializer,
            Scope = _scope,
            IndexValues = new Dictionary<string, object?>
            {
                ["session_id"] = attachment.SessionId.ToString(),
                ["source_hash"] = attachment.SourceHash
            }
        };

        return _quotaStorage.PutIfCanAccommodateAsync(request, cancellationToken);
    }

    public static IndexedDbStoreDefinition CreateStoreDefinition(string name = "attachments")
    {
        return new IndexedDbStoreDefinition
        {
            Name = name,
            KeyPath = "AttachmentId",
            AutoIncrement = false,
            Indexes = new[]
            {
                new IndexedDbIndexDefinition { Name = "session_id", KeyPath = "SessionId", Unique = false, MultiEntry = false },
                new IndexedDbIndexDefinition { Name = "source_hash", KeyPath = "SourceHash", Unique = false, MultiEntry = false }
            }
        };
    }
}
