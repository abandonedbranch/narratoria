using Narratoria.Storage;

namespace Narratoria.Storage.IndexedDb;

public sealed class IndexedDbStorageWithQuota : IIndexedDbStorageWithQuota
{
    private readonly IIndexedDbStorageService _storage;
    private readonly IStorageQuotaAwareness _quota;
    private readonly ILogger<IndexedDbStorageWithQuota> _logger;

    public IndexedDbStorageWithQuota(IIndexedDbStorageService storage, IStorageQuotaAwareness quota, ILogger<IndexedDbStorageWithQuota> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _quota = quota ?? throw new ArgumentNullException(nameof(quota));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<StorageResult<Unit>> PutIfCanAccommodateAsync<T>(IndexedDbPutRequest<T> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Serializer);
        cancellationToken.ThrowIfCancellationRequested();

        IndexedDbSerializedValue serialized;
        try
        {
            serialized = await request.Serializer.SerializeAsync(request.Value, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = StorageError.Serialization("Failed to serialize payload", ex.Message);
            _logger.LogWarning(ex, "Quota-gated IndexedDB put serialization failure store={Store} key={Key}", request.Store.Name, request.Key);
            return StorageResult<Unit>.Failure(error);
        }

        if (serialized.Payload is null)
        {
            return StorageResult<Unit>.Failure(StorageError.Serialization("Serializer returned null payload"));
        }

        var requestedBytes = serialized.SizeBytes;
        var hints = new StorageQuotaEstimationHints(request.Store.Name, null);
        var quotaResult = await _quota.CheckAsync(request.Scope, requestedBytes, hints, cancellationToken).ConfigureAwait(false);
        if (!quotaResult.Ok)
        {
            return StorageResult<Unit>.Failure(quotaResult.Error ?? StorageError.QuotaUnavailable("Quota provider unavailable"));
        }

        if (quotaResult.Value.CanAccommodate is false)
        {
            return StorageResult<Unit>.Failure(StorageError.Quota("Requested bytes exceed available quota"));
        }

        var putRequest = new IndexedDbPutSerializedRequest
        {
            Store = request.Store,
            Key = request.Key,
            Payload = serialized.Payload,
            SizeBytes = serialized.SizeBytes,
            IndexValues = request.IndexValues,
            Scope = request.Scope
        };

        return await _storage.PutSerializedAsync(putRequest, cancellationToken).ConfigureAwait(false);
    }
}
