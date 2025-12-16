namespace Narratoria.Storage.IndexedDb;

public interface IIndexedDbStorageWithQuota
{
    ValueTask<StorageResult<Unit>> PutIfCanAccommodateAsync<T>(IndexedDbPutRequest<T> request, CancellationToken cancellationToken);
}
