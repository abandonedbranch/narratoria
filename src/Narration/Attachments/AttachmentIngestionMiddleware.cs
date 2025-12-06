using Narratoria.OpenAi;

namespace Narratoria.Narration.Attachments;

public sealed class AttachmentIngestionMiddleware
{
    private const string Stage = "attachment_ingestion";

    private readonly IAttachmentIngestionService _service;
    private readonly string _attachmentId;
    private readonly AttachmentIngestionOptions _options;

    public AttachmentIngestionMiddleware(
        string attachmentId,
        IAttachmentIngestionService service,
        AttachmentIngestionOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(attachmentId))
        {
            throw new ArgumentException("Attachment id is required.", nameof(attachmentId));
        }

        _service = service ?? throw new ArgumentNullException(nameof(service));
        _attachmentId = attachmentId;
        _options = options ?? AttachmentIngestionOptions.Default;
    }

    public ValueTask<MiddlewareResult> InvokeAsync(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
    {
        return InvokeInternalAsync();

        async ValueTask<MiddlewareResult> InvokeInternalAsync()
        {
            var command = new AttachmentIngestionCommand(context.SessionId, _attachmentId, context.Trace, _options);
            var ingestionResult = await _service.IngestAsync(command, cancellationToken).ConfigureAwait(false);
            if (!ingestionResult.Ok)
            {
                var message = ingestionResult.Error?.Message ?? "Attachment ingestion failed.";
                var error = new NarrationPipelineError(NarrationPipelineErrorClass.ProviderError, message, context.SessionId, context.Trace, Stage);
                throw new NarrationPipelineException(error);
            }

            return await next(context, result, cancellationToken).ConfigureAwait(false);
        }
    }
}
