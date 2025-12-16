using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Narratoria.OpenAi;

public sealed class LoggingOpenAiApiServiceMetrics : IOpenAiApiServiceMetrics
{
    private static readonly Meter Meter = new("Narratoria.OpenAi.Api");
    private static readonly Histogram<double> LatencyMs = Meter.CreateHistogram<double>("openai_request_latency_ms");
    private static readonly Counter<long> BytesSent = Meter.CreateCounter<long>("openai_request_bytes_sent");
    private static readonly Counter<long> BytesReceived = Meter.CreateCounter<long>("openai_request_bytes_received");
    private static readonly Counter<long> RequestCount = Meter.CreateCounter<long>("openai_request_count");

    private readonly ILogger<LoggingOpenAiApiServiceMetrics> _logger;

    public LoggingOpenAiApiServiceMetrics(ILogger<LoggingOpenAiApiServiceMetrics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordRequest(string status, string errorClass)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("status", status),
            new("error_class", errorClass)
        };
        RequestCount.Add(1, tags);
        _logger.LogInformation("OpenAI request status={Status} errorClass={ErrorClass}", status, errorClass);
    }

    public void RecordLatency(TimeSpan duration)
    {
        LatencyMs.Record(duration.TotalMilliseconds);
    }

    public void RecordBytesSent(long bytes)
    {
        BytesSent.Add(bytes);
    }

    public void RecordBytesReceived(long bytes)
    {
        BytesReceived.Add(bytes);
    }
}
