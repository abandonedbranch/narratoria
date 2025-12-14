namespace Narratoria.Storage.IndexedDb;

public interface IIndexedDbStorageMetrics
{
    void RecordLatency(string operation, string store, TimeSpan duration);

    void RecordResult(string operation, string store, string status, string errorClass);

    void RecordBytesWritten(long bytes);

    void RecordBytesRead(long bytes);
}

public interface IIndexedDbStorageService
{
    ValueTask<StorageResult<Unit>> PutAsync<T>(IndexedDbPutRequest<T> request, CancellationToken cancellationToken);

    ValueTask<StorageResult<Unit>> PutSerializedAsync(IndexedDbPutSerializedRequest request, CancellationToken cancellationToken);

    ValueTask<StorageResult<IReadOnlyList<IndexedDbRecord<T>>>> ListAsync<T>(IndexedDbListRequest<T> request, CancellationToken cancellationToken);
}
