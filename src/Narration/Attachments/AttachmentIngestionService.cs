using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Narratoria.OpenAi;
using Narratoria.Storage;

namespace Narratoria.Narration.Attachments;

public sealed class AttachmentIngestionService : IAttachmentIngestionService
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly IAttachmentUploadStore _uploads;
    private readonly IProcessedAttachmentStore _processed;
    private readonly IOpenAiApiService _openAi;
    private readonly IAttachmentOpenAiContextFactory _contextFactory;
    private readonly IAttachmentIngestionMetrics _metrics;
    private readonly ILogger<AttachmentIngestionService> _logger;

    public AttachmentIngestionService(
        IAttachmentUploadStore uploads,
        IProcessedAttachmentStore processed,
        IOpenAiApiService openAi,
        IAttachmentOpenAiContextFactory contextFactory,
        ILogger<AttachmentIngestionService> logger,
        IAttachmentIngestionMetrics? metrics = null)
    {
        _uploads = uploads ?? throw new ArgumentNullException(nameof(uploads));
        _processed = processed ?? throw new ArgumentNullException(nameof(processed));
        _openAi = openAi ?? throw new ArgumentNullException(nameof(openAi));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics ?? NullAttachmentIngestionMetrics.Instance;
    }

    public async ValueTask<AttachmentIngestionResult> IngestAsync(AttachmentIngestionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var options = command.Options ?? AttachmentIngestionOptions.Default;

        var upload = await _uploads.GetAsync(command.SessionId, command.AttachmentId, cancellationToken).ConfigureAwait(false);
        if (upload is null)
        {
            var error = new AttachmentIngestionError("AttachmentNotFound", "Attachment not found for session.");
            _metrics.RecordProcessed("failure", error.ErrorClass);
            return AttachmentIngestionResult.Failure(error);
        }

        var traceId = command.Trace.TraceId;
        var requestId = command.Trace.RequestId;
        _logger.LogInformation(
            "Attachment ingestion start trace={TraceId} request={RequestId} session={SessionId} attachment={AttachmentId} mime={MimeType} size={SizeBytes}",
            traceId,
            requestId,
            command.SessionId,
            upload.AttachmentId,
            upload.MimeType,
            upload.SizeBytes);
        _metrics.RecordBytesIngested(upload.SizeBytes);

        var purgeRequested = false;
        try
        {
            var validation = Validate(upload, options);
            if (validation is not null)
            {
                _metrics.RecordProcessed("failure", validation.ErrorClass);
                return AttachmentIngestionResult.Failure(validation);
            }

            var sourceHash = ComputeHash(upload.Content);
            if (options.DedupeByHash)
            {
                var existing = await _processed.FindByHashAsync(command.SessionId, sourceHash, cancellationToken).ConfigureAwait(false);
                if (existing is not null)
                {
                    _metrics.RecordProcessed("success", "deduped");
                    return AttachmentIngestionResult.Success(existing);
                }
            }

            var inputTextResult = TryDecodeText(upload);
            if (!inputTextResult.Ok)
            {
                _metrics.RecordProcessed("failure", inputTextResult.Error!.ErrorClass);
                return AttachmentIngestionResult.Failure(inputTextResult.Error!);
            }

            var prompt = BuildPrompt(upload.FileName, upload.MimeType, inputTextResult.Value);
            var requestContext = _contextFactory.Create(command.Trace);
            var summarizeResult = await SummarizeAsync(prompt, requestContext, options, cancellationToken).ConfigureAwait(false);
            if (!summarizeResult.Ok)
            {
                _metrics.RecordProcessed("failure", summarizeResult.Error!.ErrorClass);
                return AttachmentIngestionResult.Failure(summarizeResult.Error!);
            }

            var processed = new ProcessedAttachment(
                upload.AttachmentId,
                command.SessionId,
                upload.FileName,
                upload.MimeType,
                sourceHash,
                summarizeResult.Value.NormalizedText,
                summarizeResult.Value.TokenEstimate,
                summarizeResult.Value.Truncated,
                DateTimeOffset.UtcNow);

            var persistStopwatch = Stopwatch.StartNew();
            var saveResult = await _processed.SaveAsync(processed, cancellationToken).ConfigureAwait(false);
            persistStopwatch.Stop();
            _metrics.RecordPersistenceLatency(persistStopwatch.Elapsed);

            if (!saveResult.Ok)
            {
                var error = new AttachmentIngestionError("PersistenceError", saveResult.Error?.Message ?? "Unable to persist processed attachment.");
                _metrics.RecordProcessed("failure", error.ErrorClass);
                _logger.LogWarning(
                    "Attachment ingestion persistence failure trace={TraceId} request={RequestId} session={SessionId} attachment={AttachmentId} errorClass={ErrorClass} message={Message}",
                    traceId,
                    requestId,
                    command.SessionId,
                    upload.AttachmentId,
                    error.ErrorClass,
                    error.Message);
                return AttachmentIngestionResult.Failure(error);
            }

            _metrics.RecordProcessed("success", "none");
            purgeRequested = true;
            _logger.LogInformation(
                "Attachment ingestion success trace={TraceId} request={RequestId} session={SessionId} attachment={AttachmentId}",
                traceId,
                requestId,
                command.SessionId,
                upload.AttachmentId);
            return AttachmentIngestionResult.Success(processed);
        }
        finally
        {
            try
            {
                await _uploads.DeleteAsync(command.SessionId, upload.AttachmentId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Attachment ingestion purge failed trace={TraceId} request={RequestId} session={SessionId} attachment={AttachmentId}",
                    traceId,
                    requestId,
                    command.SessionId,
                    upload.AttachmentId);
            }

            if (!purgeRequested)
            {
                _logger.LogDebug(
                    "Attachment ingestion ensured purge trace={TraceId} request={RequestId} session={SessionId} attachment={AttachmentId}",
                    traceId,
                    requestId,
                    command.SessionId,
                    upload.AttachmentId);
            }
        }
    }

    private static AttachmentIngestionError? Validate(UploadedFile upload, AttachmentIngestionOptions options)
    {
        if (!options.AllowedMimeTypes.Contains(upload.MimeType))
        {
            return new AttachmentIngestionError("UnsupportedFileType", $"Files of type {upload.MimeType} are not supported.");
        }

        if (upload.SizeBytes > options.MaxBytes)
        {
            return new AttachmentIngestionError("FileTooLarge", $"Attachment exceeds the maximum size of {options.MaxBytes} bytes.");
        }

        return null;
    }

    private static string ComputeHash(byte[] content)
    {
        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash);
    }

    private static Result<string> TryDecodeText(UploadedFile upload)
    {
        try
        {
            var text = Utf8.GetString(upload.Content);
            return Result<string>.Success(text);
        }
        catch (DecoderFallbackException ex)
        {
            var error = new AttachmentIngestionError("DecodeError", $"Attachment contents could not be decoded: {ex.Message}");
            return Result<string>.Failure(error);
        }
    }

    private async ValueTask<Result<AttachmentSummary>> SummarizeAsync(
        string prompt,
        OpenAiRequestContext context,
        AttachmentIngestionOptions options,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var builder = new StringBuilder();
        var truncated = false;
        var streamedTokens = 0;

        try
        {
            var serialized = new SerializedPrompt(Guid.NewGuid(), prompt, null);
            await foreach (var token in _openAi.StreamAsync(serialized, context, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                streamedTokens++;

                if (string.IsNullOrEmpty(token.Content))
                {
                    continue;
                }

                var content = token.Content;
                if (options.MaxChars is int maxChars && builder.Length + content.Length > maxChars)
                {
                    var remaining = maxChars - builder.Length;
                    if (remaining > 0)
                    {
                        builder.Append(content.AsSpan(0, remaining));
                    }

                    truncated = true;
                    break;
                }

                builder.Append(content);

                if (options.MaxTokens is int maxTokens && streamedTokens >= maxTokens)
                {
                    truncated = true;
                    break;
                }
            }

            return Result<AttachmentSummary>.Success(new AttachmentSummary(builder.ToString(), EstimateTokens(builder, streamedTokens), truncated));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (OpenAiApiException ex)
        {
            var error = new AttachmentIngestionError(ex.Error.ErrorClass.ToString(), ex.Error.Message);
            return Result<AttachmentSummary>.Failure(error);
        }
        catch (Exception ex)
        {
            var error = new AttachmentIngestionError("ProviderError", $"Attachment could not be ingested: {ex.Message}");
            return Result<AttachmentSummary>.Failure(error);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordProviderLatency(stopwatch.Elapsed);
        }
    }

    private static int EstimateTokens(StringBuilder builder, int streamedTokens)
    {
        if (streamedTokens > 0)
        {
            return streamedTokens;
        }

        return Math.Max(1, builder.Length / 4);
    }

    private static string BuildPrompt(string fileName, string mimeType, string contents)
    {
        var builder = new StringBuilder(contents.Length + 256);
        builder.AppendLine("Rewrite the following document for machine consumption only. Remove prose and explanations. Preserve structure, code, tables, and identifiers.");
        builder.AppendLine("Optimize for downstream LLM ingestion (not humans). Output only the optimized content.");
        builder.AppendLine();
        builder.AppendLine($"File: {fileName} ({mimeType})");
        builder.AppendLine("---");
        builder.Append(contents);
        builder.AppendLine();
        builder.AppendLine("---");
        return builder.ToString();
    }

    private readonly record struct Result<T>(bool Ok, T Value, AttachmentIngestionError? Error)
    {
        public static Result<T> Success(T value) => new(true, value, null);

        public static Result<T> Failure(AttachmentIngestionError error) => new(false, default!, error);
    }

    private readonly record struct AttachmentSummary(string NormalizedText, int TokenEstimate, bool Truncated);
}
