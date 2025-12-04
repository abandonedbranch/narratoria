using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Narratoria.OpenAi;

namespace Narratoria.Narration;

public sealed class NarrationPipelineService
{
    private const string ProviderDispatchStage = "provider_dispatch";
    private const string PersistContextStage = "persist_context";

    private readonly INarrationSessionStore _sessions;
    private readonly INarrationProvider _provider;
    private readonly INarrationPipelineObserver _observer;
    private readonly ILogger<NarrationPipelineService> _logger;
    private readonly NarrationPipelineOptions _options;
    private readonly NarrationMiddlewareNext _pipeline;

    public NarrationPipelineService(
        INarrationSessionStore sessions,
        INarrationProvider provider,
        IEnumerable<NarrationMiddleware>? middleware = null,
        INarrationPipelineObserver? observer = null,
        ILogger<NarrationPipelineService>? logger = null,
        NarrationPipelineOptions? options = null)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _observer = observer ?? NullNarrationPipelineObserver.Instance;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NarrationPipelineService>.Instance;
        _options = options ?? new NarrationPipelineOptions();

        var chain = (middleware?.ToImmutableArray() ?? ImmutableArray<NarrationMiddleware>.Empty)
            .Add(ProviderDispatchAsync)
            .Add(PersistContextAsync);

        _pipeline = BuildPipeline(chain);
    }

    public async ValueTask<IAsyncEnumerable<string>> RunAsync(NarrationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = await LoadContextAsync(request, cancellationToken).ConfigureAwait(false);
        var initialResult = MiddlewareResult.FromContext(context);
        var result = await _pipeline(context, initialResult, cancellationToken).ConfigureAwait(false);
        return result.StreamedNarration;
    }

    private async ValueTask<NarrationContext> LoadContextAsync(NarrationRequest request, CancellationToken cancellationToken)
    {
        var existing = await _sessions.LoadAsync(request.SessionId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            var error = new NarrationPipelineError(
                NarrationPipelineErrorClass.MissingSession,
                "Session state is unavailable",
                request.SessionId,
                request.Trace,
                "session_load");
            _observer.OnError(error);
            throw new NarrationPipelineException(error);
        }

        return existing with
        {
            PlayerPrompt = request.PlayerPrompt,
            WorkingNarration = ImmutableArray<string>.Empty,
            Metadata = request.Metadata ?? existing.Metadata ?? ImmutableDictionary<string, string>.Empty,
            Trace = request.Trace
        };
    }

    private static NarrationMiddlewareNext BuildPipeline(ImmutableArray<NarrationMiddleware> middleware)
    {
        NarrationMiddlewareNext next = static (_, result, _) => ValueTask.FromResult(result);

        for (var i = middleware.Length - 1; i >= 0; i--)
        {
            var middlewareStep = middleware[i];
            var current = next;
            next = (context, result, cancellationToken) => middlewareStep(context, result, current, cancellationToken);
        }

        return next;
    }

    private ValueTask<MiddlewareResult> ProviderDispatchAsync(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_options.ProviderTimeout != Timeout.InfiniteTimeSpan)
        {
            timeoutCts.CancelAfter(_options.ProviderTimeout);
        }

        var completion = PumpProviderAsync(channel.Writer, context, timeoutCts, cancellationToken);
        var providerResult = new MiddlewareResult(channel.Reader.ReadAllAsync(cancellationToken), new ValueTask<NarrationContext>(completion));
        return next(context, providerResult, cancellationToken);
    }

    private async Task<NarrationContext> PumpProviderAsync(
        ChannelWriter<string> writer,
        NarrationContext context,
        CancellationTokenSource timeoutCts,
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

            await foreach (var token in _provider.StreamNarrationAsync(context, timeoutCts.Token).ConfigureAwait(false))
            {
                timeoutCts.Token.ThrowIfCancellationRequested();
                requestCancellationToken.ThrowIfCancellationRequested();

                var content = token ?? string.Empty;

                var canWrite = await writer.WaitToWriteAsync(requestCancellationToken).ConfigureAwait(false);
                if (!canWrite)
                {
                    break;
                }

                timeoutCts.Token.ThrowIfCancellationRequested();
                requestCancellationToken.ThrowIfCancellationRequested();

                await writer.WriteAsync(content, requestCancellationToken).ConfigureAwait(false);

                tokens.Add(content);
                _observer.OnTokensStreamed(context.SessionId, 1);

                timeoutCts.Token.ThrowIfCancellationRequested();
                requestCancellationToken.ThrowIfCancellationRequested();
            }

            writer.TryComplete();
            _observer.OnStageCompleted(new NarrationStageTelemetry(ProviderDispatchStage, "success", errorClass, context.SessionId, context.Trace, stopwatch.Elapsed));
            return context with { WorkingNarration = tokens.ToImmutable() };
        }
        catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested && !requestCancellationToken.IsCancellationRequested)
        {
            errorClass = NarrationPipelineErrorClass.ProviderError.ToString();
            var error = new NarrationPipelineError(NarrationPipelineErrorClass.ProviderError, "Provider call timed out", context.SessionId, context.Trace, ProviderDispatchStage);
            _observer.OnError(error);
            writer.TryComplete(new NarrationPipelineException(error, oce));
            _observer.OnStageCompleted(new NarrationStageTelemetry(ProviderDispatchStage, "failure", errorClass, context.SessionId, context.Trace, stopwatch.Elapsed));
            throw new NarrationPipelineException(error, oce);
        }
        catch (OperationCanceledException oce)
        {
            writer.TryComplete(oce);
            _observer.OnStageCompleted(new NarrationStageTelemetry(ProviderDispatchStage, "canceled", "OperationCanceled", context.SessionId, context.Trace, stopwatch.Elapsed));
            throw;
        }
        catch (JsonException ex)
        {
            errorClass = NarrationPipelineErrorClass.DecodeError.ToString();
            var error = new NarrationPipelineError(NarrationPipelineErrorClass.DecodeError, "Unable to decode provider response", context.SessionId, context.Trace, ProviderDispatchStage);
            _observer.OnError(error);
            writer.TryComplete(new NarrationPipelineException(error, ex));
            _observer.OnStageCompleted(new NarrationStageTelemetry(ProviderDispatchStage, "failure", errorClass, context.SessionId, context.Trace, stopwatch.Elapsed));
            throw new NarrationPipelineException(error, ex);
        }
        catch (Exception ex)
        {
            errorClass = NarrationPipelineErrorClass.ProviderError.ToString();
            var error = new NarrationPipelineError(NarrationPipelineErrorClass.ProviderError, "Provider call failed", context.SessionId, context.Trace, ProviderDispatchStage);
            _observer.OnError(error);
            writer.TryComplete(new NarrationPipelineException(error, ex));
            _observer.OnStageCompleted(new NarrationStageTelemetry(ProviderDispatchStage, "failure", errorClass, context.SessionId, context.Trace, stopwatch.Elapsed));
            throw new NarrationPipelineException(error, ex);
        }
    }

    private ValueTask<MiddlewareResult> PersistContextAsync(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
    {
        return PersistInternalAsync();

        async ValueTask<MiddlewareResult> PersistInternalAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var downstream = await next(context, result, cancellationToken).ConfigureAwait(false);

            async IAsyncEnumerable<string> StreamWithPersistence([EnumeratorCancellation] CancellationToken ct)
            {
                await using var enumerator = downstream.StreamedNarration.WithCancellation(ct).GetAsyncEnumerator();
                while (true)
                {
                    bool hasNext;
                    try
                    {
                        hasNext = await enumerator.MoveNextAsync();
                    }
                    catch (TaskCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(ct);
                    }

                    if (!hasNext)
                    {
                        break;
                    }

                    yield return enumerator.Current;
                }

                await PersistWhenCompleteAsync(downstream, context, stopwatch, ct).ConfigureAwait(false);
            }

            return new MiddlewareResult(StreamWithPersistence(cancellationToken), downstream.UpdatedContext);
        }
    }

    private async Task PersistWhenCompleteAsync(MiddlewareResult downstream, NarrationContext context, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        try
        {
            var updatedContext = await downstream.UpdatedContext.ConfigureAwait(false);
            var mergedNarration = updatedContext.PriorNarration.AddRange(updatedContext.WorkingNarration);
            var persisted = updatedContext with
            {
                PriorNarration = mergedNarration,
                WorkingNarration = ImmutableArray<string>.Empty
            };
            await _sessions.SaveAsync(persisted, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _observer.OnStageCompleted(new NarrationStageTelemetry(PersistContextStage, "success", "none", context.SessionId, context.Trace, stopwatch.Elapsed));
        }
    }
}
