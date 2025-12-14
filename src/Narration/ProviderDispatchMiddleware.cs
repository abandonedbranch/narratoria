using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Narratoria.Narration;

public sealed class ProviderDispatchMiddleware
{
    private const string Stage = "provider_dispatch";

    private static readonly Meter Meter = new("Narratoria.Narration.ProviderDispatch");
    private static readonly Histogram<double> ProviderLatency = Meter.CreateHistogram<double>("provider_latency_ms");
    private static readonly Counter<long> ProviderErrorCount = Meter.CreateCounter<long>("provider_error_count");
    private static readonly Counter<long> TokensStreamed = Meter.CreateCounter<long>("tokens_streamed");

    private readonly INarrationProvider _provider;
    private readonly ProviderDispatchOptions _options;
    private readonly INarrationPipelineObserver _observer;
    private readonly ILogger<ProviderDispatchMiddleware> _logger;

    public ProviderDispatchMiddleware(
        INarrationProvider provider,
        ProviderDispatchOptions? options = null,
        INarrationPipelineObserver? observer = null,
        ILogger<ProviderDispatchMiddleware>? logger = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _options = options ?? new ProviderDispatchOptions();
        _observer = observer ?? NullNarrationPipelineObserver.Instance;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ProviderDispatchMiddleware>.Instance;
    }

    public ValueTask<MiddlewareResult> InvokeAsync(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
    {
        return InvokeInternalAsync();

        ValueTask<MiddlewareResult> InvokeInternalAsync()
        {
            cancellationToken.ThrowIfCancellationRequested();

            var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

            var timeoutOnlyCts = new CancellationTokenSource();
            if (_options.Timeout != Timeout.InfiniteTimeSpan)
            {
                timeoutOnlyCts.CancelAfter(_options.Timeout);
            }

            var runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutOnlyCts.Token);
            var completion = PumpProviderAsync(channel.Writer, context, runCts, timeoutOnlyCts, cancellationToken);

            var stream = ReadAllWithUpstreamCancellation(channel.Reader, runCts, cancellationToken);
            var providerResult = new MiddlewareResult(stream, new ValueTask<NarrationContext>(completion));
            return next(context, providerResult, cancellationToken);
        }
    }

    private static async IAsyncEnumerable<string> ReadAllWithUpstreamCancellation(
        ChannelReader<string> reader,
        CancellationTokenSource upstream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var completed = false;
        await using var enumerator = reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator();

        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                if (!hasNext)
                {
                    completed = true;
                    yield break;
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            if (!completed)
            {
                try
                {
                    upstream.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }

    private async Task<NarrationContext> PumpProviderAsync(
        ChannelWriter<string> writer,
        NarrationContext context,
        CancellationTokenSource runCts,
        CancellationTokenSource timeoutOnlyCts,
        CancellationToken requestCancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var tokens = ImmutableArray.CreateBuilder<string>();
        var errorClass = "none";
        try
        {
            _logger.LogInformation(
                "Narration provider start trace={TraceId} request={RequestId} session={SessionId}",
                context.Trace.TraceId,
                context.Trace.RequestId,
                context.SessionId);

            await foreach (var token in _provider.StreamNarrationAsync(context, runCts.Token).ConfigureAwait(false))
            {
                runCts.Token.ThrowIfCancellationRequested();
                requestCancellationToken.ThrowIfCancellationRequested();

                var content = token ?? string.Empty;

                var canWrite = await writer.WaitToWriteAsync(requestCancellationToken).ConfigureAwait(false);
                if (!canWrite)
                {
                    break;
                }

                runCts.Token.ThrowIfCancellationRequested();
                requestCancellationToken.ThrowIfCancellationRequested();

                await writer.WriteAsync(content, requestCancellationToken).ConfigureAwait(false);

                tokens.Add(content);
                _observer.OnTokensStreamed(context.SessionId, 1);
                TokensStreamed.Add(1);

                runCts.Token.ThrowIfCancellationRequested();
                requestCancellationToken.ThrowIfCancellationRequested();
            }

            writer.TryComplete();
            _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "success", errorClass, context.SessionId, context.Trace, stopwatch.Elapsed));
            RecordMetrics("success", errorClass, stopwatch.Elapsed);
            return context with { WorkingNarration = tokens.ToImmutable() };
        }
        catch (OperationCanceledException oce) when (timeoutOnlyCts.IsCancellationRequested && !requestCancellationToken.IsCancellationRequested)
        {
            errorClass = NarrationPipelineErrorClass.ProviderTimeout.ToString();
            var error = new NarrationPipelineError(NarrationPipelineErrorClass.ProviderTimeout, "Provider call timed out", context.SessionId, context.Trace, Stage);
            _observer.OnError(error);
            writer.TryComplete(new NarrationPipelineException(error, oce));
            _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "failure", errorClass, context.SessionId, context.Trace, stopwatch.Elapsed));
            RecordMetrics("failure", errorClass, stopwatch.Elapsed);
            ProviderErrorCount.Add(1, new TagList { { "error_class", errorClass } });
            throw new NarrationPipelineException(error, oce);
        }
        catch (OperationCanceledException oce)
        {
            writer.TryComplete(oce);
            _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "canceled", "OperationCanceled", context.SessionId, context.Trace, stopwatch.Elapsed));
            RecordMetrics("canceled", "OperationCanceled", stopwatch.Elapsed);
            throw;
        }
        catch (JsonException ex)
        {
            errorClass = NarrationPipelineErrorClass.DecodeError.ToString();
            var error = new NarrationPipelineError(NarrationPipelineErrorClass.DecodeError, "Unable to decode provider response", context.SessionId, context.Trace, Stage);
            _observer.OnError(error);
            writer.TryComplete(new NarrationPipelineException(error, ex));
            _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "failure", errorClass, context.SessionId, context.Trace, stopwatch.Elapsed));
            RecordMetrics("failure", errorClass, stopwatch.Elapsed);
            ProviderErrorCount.Add(1, new TagList { { "error_class", errorClass } });
            throw new NarrationPipelineException(error, ex);
        }
        catch (Exception ex)
        {
            errorClass = NarrationPipelineErrorClass.ProviderError.ToString();
            var error = new NarrationPipelineError(NarrationPipelineErrorClass.ProviderError, "Provider call failed", context.SessionId, context.Trace, Stage);
            _observer.OnError(error);
            writer.TryComplete(new NarrationPipelineException(error, ex));
            _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "failure", errorClass, context.SessionId, context.Trace, stopwatch.Elapsed));
            RecordMetrics("failure", errorClass, stopwatch.Elapsed);
            ProviderErrorCount.Add(1, new TagList { { "error_class", errorClass } });
            throw new NarrationPipelineException(error, ex);
        }
        finally
        {
            runCts.Dispose();
            timeoutOnlyCts.Dispose();
        }
    }

    private static void RecordMetrics(string status, string errorClass, TimeSpan elapsed)
    {
        var tags = new TagList
        {
            { "status", status },
            { "error_class", errorClass }
        };

        ProviderLatency.Record(elapsed.TotalMilliseconds, tags);
    }
}
