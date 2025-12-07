using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Narratoria.Narration;

public sealed class NarrationPersistenceMiddleware
{
    private const string LoadStage = "session_load";
    private const string PersistStage = "persist_context";

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
                var error = new NarrationPipelineError(NarrationPipelineErrorClass.MissingSession, "Session state is unavailable", context.SessionId, context.Trace, LoadStage);
                _observer.OnError(error);
                _observer.OnStageCompleted(new NarrationStageTelemetry(LoadStage, "failure", NarrationPipelineErrorClass.MissingSession.ToString(), context.SessionId, context.Trace, loadStopwatch.Elapsed));
                throw new NarrationPipelineException(error);
            }

            var mergedContext = loaded with
            {
                PlayerPrompt = context.PlayerPrompt,
                Metadata = context.Metadata ?? loaded.Metadata ?? ImmutableDictionary<string, string>.Empty,
                Trace = context.Trace,
                WorkingNarration = ImmutableArray<string>.Empty
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
        var completed = false;
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
                    completed = true;
                    break;
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            if (completed)
            {
                await persistenceTask.Value.ConfigureAwait(false);
            }
        }
    }

    private async Task<NarrationContext> PersistWhenCompleteAsync(
        MiddlewareResult downstream,
        NarrationContext context,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedContext = await downstream.UpdatedContext.ConfigureAwait(false);
            var targetTrace = updatedContext.Trace;
            var targetSessionId = updatedContext.SessionId;
            var mergedNarration = updatedContext.PriorNarration.AddRange(updatedContext.WorkingNarration);
            var persistable = updatedContext with
            {
                PriorNarration = mergedNarration,
                WorkingNarration = ImmutableArray<string>.Empty
            };

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
}
