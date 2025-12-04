using Narratoria.Storage;

namespace Narratoria.Storage.IndexedDb;

public sealed class IndexedDbStorageQuotaProvider : IStorageQuotaProvider
{
    private readonly IStorageQuotaEstimator _estimator;
    private bool? _isSupported;

    public IndexedDbStorageQuotaProvider(IStorageQuotaEstimator estimator)
    {
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
    }

    public async ValueTask<StorageResult<StorageQuotaEstimate>> EstimateAsync(StorageScope scope, StorageQuotaEstimationHints? hints, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_isSupported == false)
        {
            return StorageResult<StorageQuotaEstimate>.Failure(StorageError.NotSupported("StorageManager is not supported"));
        }

        StorageResult<StorageEstimateSnapshot> snapshotResult;
        try
        {
            snapshotResult = await _estimator.EstimateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return StorageResult<StorageQuotaEstimate>.Failure(StorageError.ProviderFailure("Quota estimation failed", ex.Message));
        }

        if (!snapshotResult.Ok)
        {
            if (snapshotResult.Error?.ErrorClass == StorageErrorClass.NotSupported)
            {
                _isSupported = false;
            }

            return StorageResult<StorageQuotaEstimate>.Failure(snapshotResult.Error ?? StorageError.ProviderFailure("Quota estimation failed"));
        }

        _isSupported = true;

        var snapshot = snapshotResult.Value;
        if (snapshot.UsageBytes is null || snapshot.QuotaBytes is null)
        {
            return StorageResult<StorageQuotaEstimate>.Failure(StorageError.EstimateUnavailable("Quota or usage unavailable"));
        }

        var source = string.IsNullOrWhiteSpace(snapshot.Source) ? "storage-manager" : snapshot.Source;
        return StorageResult<StorageQuotaEstimate>.Success(new StorageQuotaEstimate(snapshot.UsageBytes, snapshot.QuotaBytes, source, "indexeddb"));
    }
}
