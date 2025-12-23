using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Narratoria.OpenAi;
using Narratoria.Narration;

namespace Narratoria.Narration;

public sealed record TitleOptions(string Model, int MaxChars, int MaxTokens)
{
    public static TitleOptions Default { get; } = new("gpt-4o-mini", 64, 128);
}

public sealed class NarrationSessionTitleMiddleware
{
    private const string Stage = "session_title_update";

    private static readonly Meter Meter = new("Narratoria.Narration.SessionTitle");
    private static readonly Histogram<double> TitleLatency = Meter.CreateHistogram<double>("session_title_update_latency_ms");
    private static readonly Counter<long> TitleCount = Meter.CreateCounter<long>("session_title_update_count");

    private readonly INarrationSessionStore _sessions;
    private readonly Narratoria.OpenAi.IOpenAiApiService _openAi;
    private readonly INarrationOpenAiContextFactory _openAiContextFactory;
    private readonly INarrationPipelineObserver _observer;
    private readonly TitleOptions _options;
    private readonly ILogger<NarrationSessionTitleMiddleware> _logger;
    private readonly ProviderDispatchOptions _dispatchOptions;

    public NarrationSessionTitleMiddleware(
        INarrationSessionStore sessions,
        Narratoria.OpenAi.IOpenAiApiService openAi,
        INarrationOpenAiContextFactory openAiContextFactory,
        INarrationPipelineObserver? observer = null,
        TitleOptions? options = null,
        ILogger<NarrationSessionTitleMiddleware>? logger = null,
        ProviderDispatchOptions? dispatchOptions = null)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _openAi = openAi ?? throw new ArgumentNullException(nameof(openAi));
        _openAiContextFactory = openAiContextFactory ?? throw new ArgumentNullException(nameof(openAiContextFactory));
        _observer = observer ?? NullNarrationPipelineObserver.Instance;
        _options = options ?? TitleOptions.Default;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NarrationSessionTitleMiddleware>.Instance;
        _dispatchOptions = dispatchOptions ?? new ProviderDispatchOptions();
    }

    public ValueTask<MiddlewareResult> InvokeAsync(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
    {
        return InvokeInternalAsync();

        async ValueTask<MiddlewareResult> InvokeInternalAsync()
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stopwatch = Stopwatch.StartNew();

            // Fast path: skip if user already set a title
            var sessions = await _sessions.ListSessionsAsync(cancellationToken).ConfigureAwait(false);
            var record = sessions.FirstOrDefault(s => s.SessionId == context.SessionId);
            if (record is null)
            {
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", "MissingSession", context.SessionId, context.Trace, stopwatch.Elapsed));
                RecordMetrics("skipped", "MissingSession", stopwatch.Elapsed);
                return await next(context, result, cancellationToken).ConfigureAwait(false);
            }

            if (record.IsTitleUserSet)
            {
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", "user_title_guard", context.SessionId, context.Trace, stopwatch.Elapsed));
                RecordMetrics("skipped", "none", stopwatch.Elapsed);
                return await next(context, result, cancellationToken).ConfigureAwait(false);
            }

            // Ensure we have enough content: await final context (post-dispatch)
            NarrationContext final;
            var modelUsed = _dispatchOptions.SystemModel;
            try
            {
                final = await result.UpdatedContext.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", "OperationCanceled", context.SessionId, context.Trace, stopwatch.Elapsed));
                RecordMetrics("skipped", "OperationCanceled", stopwatch.Elapsed);
                return await next(context, result, cancellationToken).ConfigureAwait(false);
            }
            catch (NarrationPipelineException ex)
            {
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", ex.Error.ErrorClass.ToString(), context.SessionId, context.Trace, stopwatch.Elapsed));
                RecordMetrics("skipped", ex.Error.ErrorClass.ToString(), stopwatch.Elapsed);
                return await next(context, result, cancellationToken).ConfigureAwait(false);
            }

            var text = final.PriorNarration.IsDefaultOrEmpty ? string.Empty : string.Concat(final.PriorNarration);
            if (string.IsNullOrWhiteSpace(text))
            {
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", "none", context.SessionId, context.Trace, stopwatch.Elapsed));
                RecordMetrics("skipped", "none", stopwatch.Elapsed);
                return await next(context, result, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                var system = $"Summarize the session in a short, user-friendly title under {_options.MaxChars} characters.";
                var payload = system + "\n\n" + text;
                var serialized = new SerializedPrompt(Guid.NewGuid(), payload, null);
                var ctx = _openAiContextFactory.Create(final, _dispatchOptions.SystemModel);
                var titleBuilder = new System.Text.StringBuilder(_options.MaxChars);
                var tokenCount = 0;
                await foreach (var tok in _openAi.StreamAsync(serialized, ctx, cancellationToken).ConfigureAwait(false))
                {
                    if (string.IsNullOrEmpty(tok.Content))
                    {
                        continue;
                    }
                    titleBuilder.Append(tok.Content);
                    tokenCount++;
                    if (tokenCount >= _options.MaxTokens)
                    {
                        break;
                    }
                    if (titleBuilder.Length >= _options.MaxChars)
                    {
                        break;
                    }
                }
                var title = titleBuilder.ToString();
                var normalized = (title ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", "none", context.SessionId, context.Trace, stopwatch.Elapsed));
                    RecordMetrics("skipped", "none", stopwatch.Elapsed);
                    return await next(context, result, cancellationToken).ConfigureAwait(false);
                }

                if (normalized.Length > _options.MaxChars)
                {
                    normalized = normalized[.._options.MaxChars];
                }

                await _sessions.RenameSessionAsync(context.SessionId, normalized, isUserSet: false, cancellationToken).ConfigureAwait(false);

                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "success", "none", context.SessionId, context.Trace, stopwatch.Elapsed));
                _logger.LogInformation("Session title updated using model={Model} trace={TraceId} request={RequestId} session={SessionId}", modelUsed, context.Trace.TraceId, context.Trace.RequestId, context.SessionId);
                RecordMetrics("success", "none", stopwatch.Elapsed, modelUsed);
            }
            catch (OperationCanceledException)
            {
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", "OperationCanceled", context.SessionId, context.Trace, stopwatch.Elapsed));
                RecordMetrics("skipped", "OperationCanceled", stopwatch.Elapsed, modelUsed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Session title update failed trace={TraceId} request={RequestId} session={SessionId}", context.Trace.TraceId, context.Trace.RequestId, context.SessionId);
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", "ProviderError", context.SessionId, context.Trace, stopwatch.Elapsed));
                RecordMetrics("skipped", "ProviderError", stopwatch.Elapsed, modelUsed);
            }

            return await next(context, result, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void RecordMetrics(string status, string errorClass, TimeSpan elapsed, string? model = null)
    {
        var tagList = new TagList
        {
            { "status", status },
            { "error_class", errorClass }
        };
        if (!string.IsNullOrWhiteSpace(model))
        {
            tagList.Add("model", model);
        }
        TitleLatency.Record(elapsed.TotalMilliseconds, tagList);
        TitleCount.Add(1, tagList);
    }
}
