using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Storage;

namespace Narratoria.Tests;

[TestClass]
public sealed class StorageQuotaAwarenessServiceTests
{
    [TestMethod]
    public async Task CheckAsync_ReturnsReport_WhenProviderSucceeds()
    {
        var provider = new FakeProvider(StorageResult<StorageQuotaEstimate>.Success(new StorageQuotaEstimate(100, 1_000, "estimate", "indexeddb")));
        var metrics = new TestQuotaMetrics();
        var service = new StorageQuotaAwarenessService(provider, metrics, NullLogger<StorageQuotaAwarenessService>.Instance);

        var scope = new StorageScope("database", "store");
        var hints = new StorageQuotaEstimationHints("store", "payload");
        var result = await service.CheckAsync(scope, 200, hints, CancellationToken.None);

        Assert.IsTrue(result.Ok);
        Assert.AreEqual(100, result.Value.UsageBytes);
        Assert.AreEqual(1_000, result.Value.QuotaBytes);
        Assert.AreEqual(900, result.Value.AvailableBytes);
        Assert.AreEqual(true, result.Value.CanAccommodate);
        Assert.AreEqual("estimate", result.Value.Source);
        Assert.AreEqual("indexeddb", result.Value.ProviderId);
        Assert.AreEqual("success", metrics.LastStatus);
        Assert.AreEqual(StorageErrorClass.None, metrics.LastErrorClass);
        Assert.AreEqual(100, metrics.LastUsedBytes);
        Assert.AreEqual(900, metrics.LastAvailableBytes);
        Assert.AreEqual(1, provider.Calls);
    }

    [TestMethod]
    public async Task CheckAsync_SetsCanAccommodateFalse_WhenRequestedExceedsAvailable()
    {
        var provider = new FakeProvider(StorageResult<StorageQuotaEstimate>.Success(new StorageQuotaEstimate(950, 1_000, "estimate", "indexeddb")));
        var metrics = new TestQuotaMetrics();
        var service = new StorageQuotaAwarenessService(provider, metrics, NullLogger<StorageQuotaAwarenessService>.Instance);

        var scope = new StorageScope("database", "store");
        var result = await service.CheckAsync(scope, 100, null, CancellationToken.None);

        Assert.IsTrue(result.Ok);
        Assert.AreEqual(50, result.Value.AvailableBytes);
        Assert.AreEqual(false, result.Value.CanAccommodate);
        Assert.AreEqual(1, provider.Calls);
        Assert.AreEqual(true, metrics.LastCanAccommodateRecorded);
    }

    [TestMethod]
    public async Task CheckAsync_ReturnsCalculationError_WhenRequestedBytesNegative()
    {
        var provider = new FakeProvider(StorageResult<StorageQuotaEstimate>.Success(new StorageQuotaEstimate(100, 1_000, "estimate", "indexeddb")));
        var metrics = new TestQuotaMetrics();
        var service = new StorageQuotaAwarenessService(provider, metrics, NullLogger<StorageQuotaAwarenessService>.Instance);

        var scope = new StorageScope("database", "store");
        var result = await service.CheckAsync(scope, -1, null, CancellationToken.None);

        Assert.IsFalse(result.Ok);
        Assert.AreEqual(StorageErrorClass.CalculationError, result.Error?.ErrorClass);
        Assert.AreEqual(0, provider.Calls);
        Assert.AreEqual(StorageErrorClass.CalculationError, metrics.LastErrorClass);
    }

    [TestMethod]
    public async Task CheckAsync_ReturnsEstimateUnavailable_WhenProviderMissingData()
    {
        var provider = new FakeProvider(StorageResult<StorageQuotaEstimate>.Success(new StorageQuotaEstimate(null, 500, "estimate", "indexeddb")));
        var metrics = new TestQuotaMetrics();
        var service = new StorageQuotaAwarenessService(provider, metrics, NullLogger<StorageQuotaAwarenessService>.Instance);

        var scope = new StorageScope("database", "store");
        var result = await service.CheckAsync(scope, null, null, CancellationToken.None);

        Assert.IsFalse(result.Ok);
        Assert.AreEqual(StorageErrorClass.EstimateUnavailable, result.Error?.ErrorClass);
        Assert.AreEqual(1, provider.Calls);
        Assert.AreEqual(StorageErrorClass.EstimateUnavailable, metrics.LastErrorClass);
    }

    [TestMethod]
    public async Task CheckAsync_PropagatesProviderError()
    {
        var providerError = StorageError.NotSupported("storage manager not available");
        var provider = new FakeProvider(StorageResult<StorageQuotaEstimate>.Failure(providerError));
        var metrics = new TestQuotaMetrics();
        var service = new StorageQuotaAwarenessService(provider, metrics, NullLogger<StorageQuotaAwarenessService>.Instance);

        var scope = new StorageScope("database", "store");
        var result = await service.CheckAsync(scope, 50, null, CancellationToken.None);

        Assert.IsFalse(result.Ok);
        Assert.AreEqual(providerError.ErrorClass, result.Error?.ErrorClass);
        Assert.AreEqual(1, provider.Calls);
        Assert.AreEqual(providerError.ErrorClass, metrics.LastErrorClass);
    }

    [TestMethod]
    public async Task CheckAsync_WrapsProviderExceptions()
    {
        var provider = new FakeProvider((_, _) => throw new InvalidOperationException("boom"));
        var metrics = new TestQuotaMetrics();
        var service = new StorageQuotaAwarenessService(provider, metrics, NullLogger<StorageQuotaAwarenessService>.Instance);

        var scope = new StorageScope("database", "store");
        var result = await service.CheckAsync(scope, null, null, CancellationToken.None);

        Assert.IsFalse(result.Ok);
        Assert.AreEqual(StorageErrorClass.ProviderFailure, result.Error?.ErrorClass);
        Assert.AreEqual(1, provider.Calls);
        Assert.AreEqual(StorageErrorClass.ProviderFailure, metrics.LastErrorClass);
    }

    private sealed class FakeProvider : IStorageQuotaProvider
    {
        private readonly Func<StorageQuotaEstimationHints?, CancellationToken, ValueTask<StorageResult<StorageQuotaEstimate>>> _estimate;

        public int Calls { get; private set; }

        public FakeProvider(StorageResult<StorageQuotaEstimate> result)
        {
            _estimate = (_, _) => ValueTask.FromResult(result);
        }

        public FakeProvider(Func<StorageQuotaEstimationHints?, CancellationToken, StorageResult<StorageQuotaEstimate>> estimate)
        {
            _estimate = (h, ct) => ValueTask.FromResult(estimate(h, ct));
        }

        public async ValueTask<StorageResult<StorageQuotaEstimate>> EstimateAsync(StorageScope scope, StorageQuotaEstimationHints? hints, CancellationToken cancellationToken)
        {
            Calls++;
            return await _estimate(hints, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class TestQuotaMetrics : IStorageQuotaMetrics
    {
        public string? LastStatus { get; private set; }
        public StorageErrorClass LastErrorClass { get; private set; } = StorageErrorClass.None;
        public long LastUsedBytes { get; private set; }
        public long LastAvailableBytes { get; private set; }
        public bool LastCanAccommodateRecorded { get; private set; }

        public void RecordLookup(TimeSpan duration, string status, StorageErrorClass errorClass, string providerId, string source)
        {
            LastStatus = status;
            LastErrorClass = errorClass;
        }

        public void RecordUsage(long usedBytes, long availableBytes, string providerId, string source)
        {
            LastUsedBytes = usedBytes;
            LastAvailableBytes = availableBytes;
        }

        public void RecordCanAccommodate(bool canAccommodate, string providerId, string source)
        {
            LastCanAccommodateRecorded = true;
        }
    }
}
