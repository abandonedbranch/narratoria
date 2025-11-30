using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Narratoria.OpenAi;

public sealed class OpenAiApiService : IOpenAiApiService
{
    private static readonly JsonSerializerOptions RequestSerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions TokenSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
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
            var request = BuildRequest(prompt, context, out var payloadSize);
            context.Metrics.RecordBytesSent(payloadSize);

            using var response = await SendAsync(context.Client, request, linkedCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var error = new OpenAiApiError(
                    OpenAiApiErrorClass.HttpError,
                    "Provider returned non-success status",
                    (int)response.StatusCode,
                    await ReadBodySafely(response.Content, linkedCts.Token));
                errorClass = error.ErrorClass.ToString();
                context.Logger.LogError("OpenAI provider failure trace={TraceId} request={RequestId} status={StatusCode}", trace.TraceId, trace.RequestId, (int)response.StatusCode);
                completionException = new OpenAiApiException(error);
                return;
            }

            context.Logger.LogInformation("OpenAI streaming trace={TraceId} request={RequestId} stage=streaming", trace.TraceId, trace.RequestId);

            await foreach (var token in ReadTokensAsync(response.Content, context.Metrics, context.Logger, trace, linkedCts.Token))
            {
                await writer.WriteAsync(token, cancellationToken).ConfigureAwait(false);
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

    private static HttpRequestMessage BuildRequest(SerializedPrompt prompt, OpenAiRequestContext context, out long payloadSize)
    {
        var payload = new OpenAiRequestPayload(prompt.Payload, prompt.Id, prompt.Metadata);
        var payloadJson = JsonSerializer.Serialize(payload, RequestSerializerOptions);
        payloadSize = Encoding.UTF8.GetByteCount(payloadJson);

        var request = new HttpRequestMessage(HttpMethod.Post, context.Endpoint)
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.Credentials.ApiKey);

        if (context.AdditionalHeaders is { Count: > 0 })
        {
            foreach (var header in context.AdditionalHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return request;
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OpenAiApiException(new OpenAiApiError(OpenAiApiErrorClass.HttpError, "Request send failed", Details: ex.Message), ex);
        }
    }

    private static async Task<string?> ReadBodySafely(HttpContent content, CancellationToken cancellationToken)
    {
        if (content is null) return null;

        try
        {
            var text = await content.ReadAsStringAsync(cancellationToken);
            if (text is null) return null;
            return text.Length > 512 ? text[..512] : text;
        }
        catch
        {
            return null;
        }
    }

    private static async IAsyncEnumerable<StreamedToken> ReadTokensAsync(
        HttpContent content,
        IOpenAiApiServiceMetrics metrics,
        ILogger logger,
        TraceMetadata trace,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            metrics.RecordBytesReceived(line.Length);

            StreamedTokenPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<StreamedTokenPayload>(line, TokenSerializerOptions);
            }
            catch (JsonException ex)
            {
                var error = new OpenAiApiError(OpenAiApiErrorClass.DecodeError, "Unable to decode provider token", Details: ex.Message);
                logger.LogError(ex, "OpenAI decode failure trace={TraceId} request={RequestId}", trace.TraceId, trace.RequestId);
                throw new OpenAiApiException(error, ex);
            }

            if (payload is null) continue;

            yield return new StreamedToken(payload.Content ?? string.Empty, payload.IsFinal, DateTimeOffset.UtcNow);
        }
    }

    private sealed record OpenAiRequestPayload(string Prompt, Guid PromptId, IReadOnlyDictionary<string, string>? Metadata);

    private sealed record StreamedTokenPayload(string? Content, bool IsFinal);
}
