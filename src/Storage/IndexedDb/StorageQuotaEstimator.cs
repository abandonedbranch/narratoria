using Narratoria.Storage;

namespace Narratoria.Storage.IndexedDb;

public interface IStorageQuotaEstimator
{
    ValueTask<StorageResult<StorageEstimateSnapshot>> EstimateAsync(CancellationToken cancellationToken);
}

public readonly record struct StorageEstimateSnapshot(long? UsageBytes, long? QuotaBytes, string Source);
