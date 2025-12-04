using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Narratoria.Storage;

public sealed class LoggingStorageQuotaMetrics : IStorageQuotaMetrics
{
    private static readonly Meter Meter = new("Narratoria.Storage.Quota");
    private static readonly Histogram<double> LookupLatencyMs = Meter.CreateHistogram<double>("quota_lookup_latency_ms");
    private static readonly Counter<long> QuotaUsedBytes = Meter.CreateCounter<long>("quota_used_bytes");
    private static readonly Counter<long> QuotaRemainingBytes = Meter.CreateCounter<long>("quota_remaining_bytes");
    private static readonly Counter<long> QuotaErrorCount = Meter.CreateCounter<long>("quota_error_count");
    private static readonly Counter<long> QuotaCanAccommodateCount = Meter.CreateCounter<long>("quota_can_accommodate_count");

    private readonly ILogger<LoggingStorageQuotaMetrics> _logger;

    public LoggingStorageQuotaMetrics(ILogger<LoggingStorageQuotaMetrics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordLookup(TimeSpan duration, string status, StorageErrorClass errorClass, string providerId, string source)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("provider_id", providerId),
            new("source", source),
            new("status", status),
            new("error_class", errorClass.ToString())
        };

        LookupLatencyMs.Record(duration.TotalMilliseconds, tags);
        if (errorClass != StorageErrorClass.None)
        {
            QuotaErrorCount.Add(1, tags);
        }

        _logger.LogInformation("Quota lookup metrics status={Status} errorClass={ErrorClass} providerId={ProviderId} source={Source} elapsedMs={ElapsedMs}", status, errorClass, providerId, source, duration.TotalMilliseconds);
    }

    public void RecordUsage(long usedBytes, long availableBytes, string providerId, string source)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("provider_id", providerId),
            new("source", source)
        };

        QuotaUsedBytes.Add(usedBytes, tags);
        QuotaRemainingBytes.Add(availableBytes, tags);
        _logger.LogInformation("Quota usage providerId={ProviderId} source={Source} usedBytes={UsedBytes} availableBytes={AvailableBytes}", providerId, source, usedBytes, availableBytes);
    }

    public void RecordCanAccommodate(bool canAccommodate, string providerId, string source)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("provider_id", providerId),
            new("source", source),
            new("can_accommodate", canAccommodate)
        };

        QuotaCanAccommodateCount.Add(1, tags);
        _logger.LogInformation("Quota can accommodate providerId={ProviderId} source={Source} canAccommodate={CanAccommodate}", providerId, source, canAccommodate);
    }
}
