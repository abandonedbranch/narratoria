using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Narratoria.Storage;

public sealed class StorageQuotaAwarenessService : IStorageQuotaAwareness
{
    private readonly IStorageQuotaProvider _provider;
    private readonly IStorageQuotaMetrics _metrics;
    private readonly ILogger<StorageQuotaAwarenessService> _logger;

    public StorageQuotaAwarenessService(IStorageQuotaProvider provider, IStorageQuotaMetrics metrics, ILogger<StorageQuotaAwarenessService> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<StorageResult<QuotaReport>> CheckAsync(StorageScope scope, long? requestedBytes, StorageQuotaEstimationHints? hints, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (requestedBytes is < 0)
        {
            var error = StorageError.Calculation("RequestedBytes must be non-negative");
            _metrics.RecordLookup(TimeSpan.Zero, "failure", error.ErrorClass, "unknown", "unknown");
            _logger.LogWarning("Quota check invalid input trace={TraceId} request={RequestId} scope={Scope} requestedBytes={RequestedBytes}", Guid.NewGuid(), Guid.NewGuid(), $"{scope.Database}/{scope.Store}", requestedBytes);
            return StorageResult<QuotaReport>.Failure(error);
        }

        var traceId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        var status = "failure";
        var errorClass = StorageErrorClass.None;
        var providerId = "unknown";
        var source = "unknown";

        _logger.LogInformation("Quota check start trace={TraceId} request={RequestId} scope={Scope} hintsStore={HintsStore}", traceId, requestId, $"{scope.Database}/{scope.Store}", hints?.StoreName);

        try
        {
            var estimateResult = await _provider.EstimateAsync(scope, hints, cancellationToken).ConfigureAwait(false);
            if (!estimateResult.Ok)
            {
                errorClass = estimateResult.Error?.ErrorClass ?? StorageErrorClass.ProviderFailure;
                var error = estimateResult.Error ?? StorageError.ProviderFailure("Quota provider unavailable");
                _logger.LogWarning("Quota estimate unavailable trace={TraceId} request={RequestId} scope={Scope} errorClass={ErrorClass}", traceId, requestId, $"{scope.Database}/{scope.Store}", error.ErrorClass);
                return StorageResult<QuotaReport>.Failure(error);
            }

            var estimate = estimateResult.Value;
            providerId = estimate.ProviderId;
            source = estimate.Source;

            if (estimate.UsageBytes is null || estimate.QuotaBytes is null)
            {
                errorClass = StorageErrorClass.EstimateUnavailable;
                var error = StorageError.EstimateUnavailable("Quota or usage not available");
                _logger.LogWarning("Quota estimate missing fields trace={TraceId} request={RequestId} scope={Scope}", traceId, requestId, $"{scope.Database}/{scope.Store}");
                return StorageResult<QuotaReport>.Failure(error);
            }

            var availableBytes = estimate.QuotaBytes.Value - estimate.UsageBytes.Value;
            var canAccommodate = requestedBytes.HasValue ? availableBytes >= requestedBytes.Value : (bool?)null;

            _metrics.RecordUsage(estimate.UsageBytes.Value, availableBytes, providerId, source);
            if (canAccommodate is not null)
            {
                _metrics.RecordCanAccommodate(canAccommodate.Value, providerId, source);
            }

            status = "success";
            errorClass = StorageErrorClass.None;
            var report = new QuotaReport(estimate.UsageBytes.Value, estimate.QuotaBytes.Value, availableBytes, canAccommodate, source, providerId);
            _logger.LogInformation("Quota check success trace={TraceId} request={RequestId} scope={Scope} providerId={ProviderId} source={Source} availableBytes={AvailableBytes}", traceId, requestId, $"{scope.Database}/{scope.Store}", providerId, source, availableBytes);
            return StorageResult<QuotaReport>.Success(report);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            status = "canceled";
            errorClass = StorageErrorClass.TransactionFailure;
            _logger.LogWarning("Quota check canceled trace={TraceId} request={RequestId} scope={Scope}", traceId, requestId, $"{scope.Database}/{scope.Store}");
            throw;
        }
        catch (Exception ex)
        {
            errorClass = StorageErrorClass.ProviderFailure;
            _logger.LogError(ex, "Quota check failed trace={TraceId} request={RequestId} scope={Scope}", traceId, requestId, $"{scope.Database}/{scope.Store}");
            return StorageResult<QuotaReport>.Failure(StorageError.ProviderFailure("Quota provider failed", ex.Message));
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordLookup(stopwatch.Elapsed, status, errorClass, providerId, source);
            _logger.LogInformation("Quota check summary trace={TraceId} request={RequestId} scope={Scope} status={Status} elapsedMs={ElapsedMs} errorClass={ErrorClass}", traceId, requestId, $"{scope.Database}/{scope.Store}", status, stopwatch.Elapsed.TotalMilliseconds, errorClass);
        }
    }
}
