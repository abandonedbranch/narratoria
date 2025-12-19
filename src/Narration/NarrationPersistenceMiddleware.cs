using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Narratoria.Narration;

public sealed class NarrationPersistenceMiddleware
{
    private const string LoadStage = "session_load";
    private const string PersistStage = "persist_context";

    private static readonly string[] EphemeralMetadataPrefixes =
    [
        "system_prompt_",
        "content_guardian_"
    ];

    private readonly INarrationSessionStore _sessions;
    private readonly INarrationPipelineObserver _observer;

    public NarrationPersistenceMiddleware(INarrationSessionStore sessions, INarrationPipelineObserver? observer = null)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _observer = observer ?? NullNarrationPipelineObserver.Instance;
    }

    public ValueTask<MiddlewareResult> InvokeAsync(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
    {
        return InvokeInternalAsync();

        async ValueTask<MiddlewareResult> InvokeInternalAsync()
        {
            cancellationToken.ThrowIfCancellationRequested();

            var loadStopwatch = Stopwatch.StartNew();
            var loaded = await _sessions.LoadAsync(context.SessionId, cancellationToken).ConfigureAwait(false);
            if (loaded is null)
            {
                var newContext = context with
                {
                    PriorNarration = ImmutableArray<string>.Empty,
                    WorkingNarration = ImmutableArray<string>.Empty,
                    WorkingContextSegments = ImmutableArray<ContextSegment>.Empty
                };

                _observer.OnStageCompleted(new NarrationStageTelemetry(LoadStage, "skipped", "none", newContext.SessionId, newContext.Trace, loadStopwatch.Elapsed));

                var newSessionDownstream = await next(newContext, MiddlewareResult.FromContext(newContext), cancellationToken).ConfigureAwait(false);

                var newSessionPersistStopwatch = Stopwatch.StartNew();
                var newSessionPersistenceTask = new Lazy<Task<NarrationContext>>(
                    () => PersistWhenCompleteAsync(newSessionDownstream, newContext, newSessionPersistStopwatch, cancellationToken),
                    LazyThreadSafetyMode.ExecutionAndPublication);

                return new MiddlewareResult(StreamWithPersistence(newSessionDownstream, newSessionPersistenceTask, cancellationToken), new ValueTask<NarrationContext>(newSessionPersistenceTask.Value));
            }

            var mergedContext = loaded with
            {
                PlayerPrompt = context.PlayerPrompt,
                Metadata = StripEphemeralMetadata(MergeMetadata(loaded.Metadata, context.Metadata)),
                Trace = context.Trace,
                WorkingNarration = ImmutableArray<string>.Empty,
                WorkingContextSegments = ImmutableArray<ContextSegment>.Empty
            };

            _observer.OnStageCompleted(new NarrationStageTelemetry(LoadStage, "success", "none", mergedContext.SessionId, mergedContext.Trace, loadStopwatch.Elapsed));

            var downstreamResult = await next(mergedContext, MiddlewareResult.FromContext(mergedContext), cancellationToken).ConfigureAwait(false);

            var persistStopwatch = Stopwatch.StartNew();
            var persistenceTask = new Lazy<Task<NarrationContext>>(
                () => PersistWhenCompleteAsync(downstreamResult, mergedContext, persistStopwatch, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication);

            return new MiddlewareResult(StreamWithPersistence(downstreamResult, persistenceTask, cancellationToken), new ValueTask<NarrationContext>(persistenceTask.Value));
        }
    }

    private static async IAsyncEnumerable<string> StreamWithPersistence(
        MiddlewareResult downstreamResult,
        Lazy<Task<NarrationContext>> persistenceTask,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var enumerator = downstreamResult.StreamedNarration.WithCancellation(cancellationToken).GetAsyncEnumerator();
        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                if (!hasNext)
                {
                    break;
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            // Always attempt persistence to emit appropriate stage telemetry
            // (success, skipped, or canceled) per spec, even when stream ends early.
            await persistenceTask.Value.ConfigureAwait(false);
        }
    }

    private async Task<NarrationContext> PersistWhenCompleteAsync(
        MiddlewareResult downstream,
        NarrationContext context,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        NarrationContext updatedContext;
        try
        {
            updatedContext = await downstream.UpdatedContext.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _observer.OnStageCompleted(new NarrationStageTelemetry(PersistStage, "canceled", "OperationCanceled", context.SessionId, context.Trace, stopwatch.Elapsed));
            throw;
        }
        catch (NarrationPipelineException ex)
        {
            _observer.OnStageCompleted(new NarrationStageTelemetry(PersistStage, "skipped", ex.Error.ErrorClass.ToString(), context.SessionId, context.Trace, stopwatch.Elapsed));
            throw;
        }

        var targetTrace = updatedContext.Trace;
        var targetSessionId = updatedContext.SessionId;
        var mergedNarration = updatedContext.PriorNarration.AddRange(updatedContext.WorkingNarration);

        var persistMetadata = StripEphemeralMetadata(updatedContext.Metadata);
        var persistable = updatedContext with
        {
            PriorNarration = mergedNarration,
            WorkingNarration = ImmutableArray<string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Metadata = persistMetadata
        };

        try
        {
            await _sessions.SaveAsync(persistable, cancellationToken).ConfigureAwait(false);
            _observer.OnStageCompleted(new NarrationStageTelemetry(PersistStage, "success", "none", targetSessionId, targetTrace, stopwatch.Elapsed));
            return persistable;
        }
        catch (OperationCanceledException)
        {
            _observer.OnStageCompleted(new NarrationStageTelemetry(PersistStage, "canceled", "OperationCanceled", context.SessionId, context.Trace, stopwatch.Elapsed));
            throw;
        }
        catch (Exception ex)
        {
            var error = new NarrationPipelineError(NarrationPipelineErrorClass.PersistenceError, "Failed to persist narration context", context.SessionId, context.Trace, PersistStage);
            _observer.OnError(error);
            _observer.OnStageCompleted(new NarrationStageTelemetry(PersistStage, "failure", NarrationPipelineErrorClass.PersistenceError.ToString(), context.SessionId, context.Trace, stopwatch.Elapsed));
            throw new NarrationPipelineException(error, ex);
        }
    }

    private static ImmutableDictionary<string, string> MergeMetadata(
        IReadOnlyDictionary<string, string> stored,
        IReadOnlyDictionary<string, string> request)
    {
        var result = stored as ImmutableDictionary<string, string>
            ?? stored.ToImmutableDictionary(StringComparer.Ordinal);

        foreach (var (key, value) in request)
        {
            result = result.SetItem(key, value);
        }

        return result;
    }

    private static ImmutableDictionary<string, string> StripEphemeralMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.Count == 0)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        var immutable = metadata as ImmutableDictionary<string, string>
            ?? metadata.ToImmutableDictionary(StringComparer.Ordinal);

        foreach (var prefix in EphemeralMetadataPrefixes)
        {
            var keysToRemove = immutable.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                immutable = immutable.Remove(key);
            }
        }

        return immutable;
    }
}
