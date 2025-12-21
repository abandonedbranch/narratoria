using System.Collections.Immutable;
using System.Diagnostics;

namespace Narratoria.Narration.Attachments;

public sealed class AttachmentContextInjectionMiddleware
{
    private const string Stage = "attachment_context_injection";

    private readonly IProcessedAttachmentStore _processed;
    private readonly IReadOnlyList<string> _attachmentIds;
    private readonly INarrationPipelineObserver _observer;

    public AttachmentContextInjectionMiddleware(
        IProcessedAttachmentStore processed,
        IReadOnlyList<string> attachmentIds,
        INarrationPipelineObserver? observer = null)
    {
        _processed = processed ?? throw new ArgumentNullException(nameof(processed));
        _attachmentIds = attachmentIds ?? Array.Empty<string>();
        _observer = observer ?? NullNarrationPipelineObserver.Instance;
    }

    public ValueTask<MiddlewareResult> InvokeAsync(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
    {
        return InvokeInternalAsync();

        async ValueTask<MiddlewareResult> InvokeInternalAsync()
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();

            if (_attachmentIds.Count == 0)
            {
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", "none", context.SessionId, context.Trace, stopwatch.Elapsed));
                return await next(context, result, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                var listResult = await _processed.ListBySessionAsync(context.SessionId, cancellationToken).ConfigureAwait(false);
                if (!listResult.Ok)
                {
                    _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", listResult.Error?.ErrorClass.ToString() ?? "PersistenceError", context.SessionId, context.Trace, stopwatch.Elapsed));
                    return await next(context, result, cancellationToken).ConfigureAwait(false);
                }

                var byId = (listResult.Value ?? Array.Empty<ProcessedAttachment>()).ToDictionary(a => a.AttachmentId, StringComparer.Ordinal);
                var segments = new List<ContextSegment>();
                foreach (var id in _attachmentIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    if (!byId.TryGetValue(id, out var attachment))
                    {
                        continue;
                    }

                    var content = BuildAttachmentSegmentContent(attachment);
                    segments.Add(new ContextSegment(Role: "system", Content: content, Source: Stage));
                }

                if (segments.Count == 0)
                {
                    _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", "none", context.SessionId, context.Trace, stopwatch.Elapsed));
                    return await next(context, result, cancellationToken).ConfigureAwait(false);
                }

                var baseline = context.WorkingContextSegments.IsDefault
                    ? ImmutableArray<ContextSegment>.Empty
                    : context.WorkingContextSegments;

                var updated = context with { WorkingContextSegments = baseline.AddRange(segments) };
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "success", "none", context.SessionId, context.Trace, stopwatch.Elapsed));
                return await next(updated, result, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "canceled", "OperationCanceled", context.SessionId, context.Trace, stopwatch.Elapsed));
                throw;
            }
            catch
            {
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", "ProviderError", context.SessionId, context.Trace, stopwatch.Elapsed));
                return await next(context, result, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static string BuildAttachmentSegmentContent(ProcessedAttachment attachment)
    {
        return $"ATTACHMENT\n" +
               $"id: {attachment.AttachmentId}\n" +
               $"name: {attachment.FileName}\n" +
               $"mime: {attachment.MimeType}\n\n" +
               (attachment.NormalizedText ?? string.Empty);
    }
}
