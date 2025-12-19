using Narratoria.OpenAi;
using System.Diagnostics;

namespace Narratoria.Narration.Attachments;

public sealed class AttachmentIngestionMiddleware
{
    private const string Stage = "attachment_ingestion";

    private readonly IAttachmentIngestionService _service;
    private readonly string _attachmentId;
    private readonly AttachmentIngestionOptions _options;
    private readonly INarrationPipelineObserver _observer;

    public AttachmentIngestionMiddleware(
        string attachmentId,
        IAttachmentIngestionService service,
        INarrationPipelineObserver? observer = null,
        AttachmentIngestionOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(attachmentId))
        {
            throw new ArgumentException("Attachment id is required.", nameof(attachmentId));
        }

        _service = service ?? throw new ArgumentNullException(nameof(service));
        _observer = observer ?? NullNarrationPipelineObserver.Instance;
        _attachmentId = attachmentId;
        _options = options ?? AttachmentIngestionOptions.Default;
    }

    public ValueTask<MiddlewareResult> InvokeAsync(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
    {
        return InvokeInternalAsync();

        async ValueTask<MiddlewareResult> InvokeInternalAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var command = new AttachmentIngestionCommand(context.SessionId, _attachmentId, context.Trace, _options);
            try
            {
                var ingestionResult = await _service.IngestAsync(command, cancellationToken).ConfigureAwait(false);
                if (!ingestionResult.Ok)
                {
                    var message = ingestionResult.Error?.Message ?? "Attachment ingestion failed.";
                    var errorClass = ingestionResult.Error?.ErrorClass ?? NarrationPipelineErrorClass.ProviderError.ToString();
                    var error = new NarrationPipelineError(NarrationPipelineErrorClass.ProviderError, $"attachment={_attachmentId} {message}", context.SessionId, context.Trace, Stage);
                    _observer.OnError(error);
                    _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "failure", errorClass, context.SessionId, context.Trace, stopwatch.Elapsed));
                    throw new NarrationPipelineException(error);
                }

                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "success", "none", context.SessionId, context.Trace, stopwatch.Elapsed));
                return await next(context, result, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "canceled", "OperationCanceled", context.SessionId, context.Trace, stopwatch.Elapsed));
                throw;
            }
        }
    }
}
