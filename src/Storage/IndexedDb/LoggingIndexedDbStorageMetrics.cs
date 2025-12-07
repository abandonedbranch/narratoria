using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Narratoria.Storage.IndexedDb;

public sealed class LoggingIndexedDbStorageMetrics : IIndexedDbStorageMetrics
{
    private static readonly Meter Meter = new("Narratoria.Storage.IndexedDb");
    private static readonly Histogram<double> LatencyMs = Meter.CreateHistogram<double>("indexeddb_operation_latency_ms");
    private static readonly Counter<long> BytesWritten = Meter.CreateCounter<long>("indexeddb_bytes_written");
    private static readonly Counter<long> BytesRead = Meter.CreateCounter<long>("indexeddb_bytes_read");
    private static readonly Counter<long> OperationCount = Meter.CreateCounter<long>("indexeddb_operation_count");

    private readonly ILogger<LoggingIndexedDbStorageMetrics> _logger;

    public LoggingIndexedDbStorageMetrics(ILogger<LoggingIndexedDbStorageMetrics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordLatency(string operation, string store, TimeSpan duration)
    {
        var tags = Tags(operation, store).ToArray();
        LatencyMs.Record(duration.TotalMilliseconds, tags);
    }

    public void RecordResult(string operation, string store, string status, string errorClass)
    {
        var tags = Tags(operation, store, status, errorClass).ToArray();
        OperationCount.Add(1, tags);
        _logger.LogInformation("IndexedDB operation={Operation} store={Store} status={Status} errorClass={ErrorClass}", operation, store, status, errorClass);
    }

    public void RecordBytesWritten(long bytes)
    {
        BytesWritten.Add(bytes);
    }

    public void RecordBytesRead(long bytes)
    {
        BytesRead.Add(bytes);
    }

    private static IEnumerable<KeyValuePair<string, object?>> Tags(string operation, string store, string? status = null, string? errorClass = null)
    {
        yield return new KeyValuePair<string, object?>("operation", operation);
        yield return new KeyValuePair<string, object?>("store", store);
        if (status is not null) yield return new KeyValuePair<string, object?>("status", status);
        if (errorClass is not null) yield return new KeyValuePair<string, object?>("error_class", errorClass);
    }
}
