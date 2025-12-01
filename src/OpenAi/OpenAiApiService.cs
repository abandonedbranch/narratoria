using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using OpenAI.Chat;

namespace Narratoria.OpenAi;

public sealed class OpenAiApiService : IOpenAiApiService
{
    private static readonly JsonSerializerOptions RequestSerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public IAsyncEnumerable<StreamedToken> StreamAsync(SerializedPrompt prompt, OpenAiRequestContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(context);

        var channel = Channel.CreateUnbounded<StreamedToken>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        _ = PumpAsync(channel.Writer, prompt, context, cancellationToken);
        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    private static async Task PumpAsync(ChannelWriter<StreamedToken> writer, SerializedPrompt prompt, OpenAiRequestContext context, CancellationToken cancellationToken)
    {
        var trace = context.Trace;
        var success = false;
        var errorClass = "none";
        var stopwatch = Stopwatch.StartNew();
        Exception? completionException = null;

        context.Logger.LogInformation("OpenAI request start trace={TraceId} request={RequestId} stage=prepare", trace.TraceId, trace.RequestId);

        using var timeoutCts = new CancellationTokenSource();
        if (context.Policy.Timeout != Timeout.InfiniteTimeSpan)
        {
            timeoutCts.CancelAfter(context.Policy.Timeout);
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var streamingProvider = context.StreamingProvider ?? throw new InvalidOperationException("Streaming provider is required.");
            var payloadSize = CalculatePayloadSize(prompt);
            context.Metrics.RecordBytesSent(payloadSize);

            context.Logger.LogInformation("OpenAI streaming trace={TraceId} request={RequestId} stage=streaming", trace.TraceId, trace.RequestId);

            await foreach (var update in streamingProvider.StreamAsync(prompt, linkedCts.Token).ConfigureAwait(false))
            {
                var content = ExtractContent(update);
                if (content.Length > 0)
                {
                    context.Metrics.RecordBytesReceived(Encoding.UTF8.GetByteCount(content));
                }

                var isFinal = update?.FinishReason is not null;
                await writer.WriteAsync(new StreamedToken(content, isFinal, DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);
            }

            success = true;
            context.Logger.LogInformation("OpenAI request completed trace={TraceId} request={RequestId}", trace.TraceId, trace.RequestId);
        }
        catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            var error = new OpenAiApiError(OpenAiApiErrorClass.NetworkTimeout, "Provider call timed out");
            errorClass = error.ErrorClass.ToString();
            context.Logger.LogWarning(oce, "OpenAI request timeout trace={TraceId} request={RequestId}", trace.TraceId, trace.RequestId);
            completionException = new OpenAiApiException(error, oce);
        }
        catch (OperationCanceledException oce)
        {
            errorClass = "OperationCanceled";
            completionException = oce;
        }
        catch (JsonException ex)
        {
            var error = new OpenAiApiError(OpenAiApiErrorClass.DecodeError, "Unable to decode provider token", Details: ex.Message);
            errorClass = error.ErrorClass.ToString();
            context.Logger.LogError(ex, "OpenAI decode failure trace={TraceId} request={RequestId}", trace.TraceId, trace.RequestId);
            completionException = new OpenAiApiException(error, ex);
        }
        catch (OpenAiApiException ex)
        {
            errorClass = ex.Error.ErrorClass.ToString();
            context.Logger.LogError(ex, "OpenAI request failed trace={TraceId} request={RequestId} errorClass={ErrorClass}", trace.TraceId, trace.RequestId, errorClass);
            completionException = ex;
        }
        catch (Exception ex)
        {
            var error = new OpenAiApiError(OpenAiApiErrorClass.HttpError, "Provider request failed", Details: ex.Message);
            errorClass = error.ErrorClass.ToString();
            context.Logger.LogError(ex, "OpenAI request failed trace={TraceId} request={RequestId}", trace.TraceId, trace.RequestId);
            completionException = new OpenAiApiException(error, ex);
        }
        finally
        {
            stopwatch.Stop();
            context.Metrics.RecordLatency(stopwatch.Elapsed);
            context.Metrics.RecordRequest(success ? "success" : "failure", success ? "none" : errorClass);
            context.Logger.LogInformation(
                "OpenAI request summary trace={TraceId} request={RequestId} status={Status} elapsedMs={ElapsedMs}",
                trace.TraceId,
                trace.RequestId,
                success ? "success" : "failure",
                stopwatch.Elapsed.TotalMilliseconds);

            if (completionException is null)
            {
                writer.TryComplete();
            }
            else
            {
                writer.TryComplete(completionException);
            }
        }
    }

    private static long CalculatePayloadSize(SerializedPrompt prompt)
    {
        var payload = new OpenAiRequestPayload(prompt.Payload, prompt.Id, prompt.Metadata);
        var payloadJson = JsonSerializer.Serialize(payload, RequestSerializerOptions);
        return Encoding.UTF8.GetByteCount(payloadJson);
    }

    private static string ExtractContent(StreamingChatCompletionUpdate? update)
    {
        if (update?.ContentUpdate is not { Count: > 0 } content)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(content.Count * 8);
        foreach (var part in content)
        {
            if (part.Kind == ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(part.Text))
            {
                builder.Append(part.Text);
            }
        }

        return builder.ToString();
    }

    private sealed record OpenAiRequestPayload(string Prompt, Guid PromptId, IReadOnlyDictionary<string, string>? Metadata);
}
