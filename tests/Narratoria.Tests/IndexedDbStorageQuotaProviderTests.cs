using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Storage;
using Narratoria.Storage.IndexedDb;

namespace Narratoria.Tests;

[TestClass]
public sealed class IndexedDbStorageQuotaProviderTests
{
    [TestMethod]
    public async Task EstimateAsync_ReturnsQuotaEstimate_WhenSupported()
    {
        var estimator = new FakeEstimator(StorageResult<StorageEstimateSnapshot>.Success(new StorageEstimateSnapshot(400, 1_200, "storage-manager")));
        var provider = new IndexedDbStorageQuotaProvider(estimator);

        var result = await provider.EstimateAsync(new StorageScope("database", "store"), null, CancellationToken.None);

        Assert.IsTrue(result.Ok);
        Assert.AreEqual(400, result.Value.UsageBytes);
        Assert.AreEqual(1_200, result.Value.QuotaBytes);
        Assert.AreEqual("storage-manager", result.Value.Source);
        Assert.AreEqual("indexeddb", result.Value.ProviderId);
        Assert.AreEqual(1, estimator.Calls);
    }

    [TestMethod]
    public async Task EstimateAsync_CachesUnsupportedCapability()
    {
        var estimator = new FakeEstimator(StorageResult<StorageEstimateSnapshot>.Failure(StorageError.NotSupported("unsupported")));
        var provider = new IndexedDbStorageQuotaProvider(estimator);

        var first = await provider.EstimateAsync(new StorageScope("database", "store"), null, CancellationToken.None);
        var second = await provider.EstimateAsync(new StorageScope("database", "store"), null, CancellationToken.None);

        Assert.IsFalse(first.Ok);
        Assert.IsFalse(second.Ok);
        Assert.AreEqual(StorageErrorClass.NotSupported, second.Error?.ErrorClass);
        Assert.AreEqual(1, estimator.Calls);
    }

    [TestMethod]
    public async Task EstimateAsync_ReturnsEstimateUnavailable_WhenUsageMissing()
    {
        var estimator = new FakeEstimator(StorageResult<StorageEstimateSnapshot>.Success(new StorageEstimateSnapshot(null, 2_000, "storage-manager")));
        var provider = new IndexedDbStorageQuotaProvider(estimator);

        var result = await provider.EstimateAsync(new StorageScope("database", "store"), null, CancellationToken.None);

        Assert.IsFalse(result.Ok);
        Assert.AreEqual(StorageErrorClass.EstimateUnavailable, result.Error?.ErrorClass);
    }

    private sealed class FakeEstimator : IStorageQuotaEstimator
    {
        private readonly StorageResult<StorageEstimateSnapshot> _result;

        public int Calls { get; private set; }

        public FakeEstimator(StorageResult<StorageEstimateSnapshot> result)
        {
            _result = result;
        }

        public ValueTask<StorageResult<StorageEstimateSnapshot>> EstimateAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return ValueTask.FromResult(_result);
        }
    }
}
