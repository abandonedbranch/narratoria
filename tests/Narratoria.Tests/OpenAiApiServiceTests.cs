using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.OpenAi;

namespace Narratoria.Tests;

[TestClass]
public sealed class OpenAiApiServiceTests
{
    [TestMethod]
    public async Task StreamAsync_YieldsTokensFromProvider()
    {
        var handler = new DelegateHandler((_, _) => Task.FromResult(HttpResponse(HttpStatusCode.OK, "{\"content\":\"hello\",\"isFinal\":false}\n{\"content\":\"world\",\"isFinal\":true}")));
        using var client = new HttpClient(handler);
        var metrics = new TestMetricsRecorder();
        var context = CreateContext(client, metrics);
        var service = new OpenAiApiService();

        var tokens = new List<StreamedToken>();
        await foreach (var token in service.StreamAsync(new SerializedPrompt(Guid.NewGuid(), "payload"), context, CancellationToken.None))
        {
            tokens.Add(token);
        }

        Assert.AreEqual(2, tokens.Count);
        Assert.AreEqual("hello", tokens[0].Content);
        Assert.IsFalse(tokens[0].IsFinal);
        Assert.AreEqual("world", tokens[1].Content);
        Assert.IsTrue(tokens[1].IsFinal);
        Assert.AreEqual("success", metrics.LastRequestStatus);
    }

    [TestMethod]
    public async Task StreamAsync_EmitsHttpError()
    {
        var handler = new DelegateHandler((_, _) => Task.FromResult(HttpResponse(HttpStatusCode.BadRequest, "boom")));
        using var client = new HttpClient(handler);
        var metrics = new TestMetricsRecorder();
        var context = CreateContext(client, metrics);
        var service = new OpenAiApiService();

        var exception = await Assert.ThrowsExceptionAsync<OpenAiApiException>(async () =>
        {
            await foreach (var _ in service.StreamAsync(new SerializedPrompt(Guid.NewGuid(), "payload"), context, CancellationToken.None))
            {
            }
        });

        Assert.AreEqual(OpenAiApiErrorClass.HttpError, exception.Error.ErrorClass);
        Assert.AreEqual("failure", metrics.LastRequestStatus);
        Assert.AreEqual("HttpError", metrics.LastErrorClass);
    }

    [TestMethod]
    public async Task StreamAsync_TimeoutsRaiseNetworkTimeout()
    {
        var handler = new DelegateHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            return HttpResponse(HttpStatusCode.OK, "{\"content\":\"payload\",\"isFinal\":true}");
        });
        using var client = new HttpClient(handler);
        var metrics = new TestMetricsRecorder();
        var context = CreateContext(client, metrics, timeout: TimeSpan.FromMilliseconds(10));
        var service = new OpenAiApiService();

        var exception = await Assert.ThrowsExceptionAsync<OpenAiApiException>(async () =>
        {
            await foreach (var _ in service.StreamAsync(new SerializedPrompt(Guid.NewGuid(), "payload"), context, CancellationToken.None))
            {
            }
        });

        Assert.AreEqual(OpenAiApiErrorClass.NetworkTimeout, exception.Error.ErrorClass);
        Assert.AreEqual("failure", metrics.LastRequestStatus);
    }

    [TestMethod]
    public async Task StreamAsync_DecodeErrorsPropagate()
    {
        var handler = new DelegateHandler((_, _) => Task.FromResult(HttpResponse(HttpStatusCode.OK, "not json")));
        using var client = new HttpClient(handler);
        var metrics = new TestMetricsRecorder();
        var context = CreateContext(client, metrics);
        var service = new OpenAiApiService();

        var exception = await Assert.ThrowsExceptionAsync<OpenAiApiException>(async () =>
        {
            await foreach (var _ in service.StreamAsync(new SerializedPrompt(Guid.NewGuid(), "payload"), context, CancellationToken.None))
            {
            }
        });

        Assert.AreEqual(OpenAiApiErrorClass.DecodeError, exception.Error.ErrorClass);
        Assert.AreEqual("failure", metrics.LastRequestStatus);
    }

    private static OpenAiRequestContext CreateContext(HttpClient client, IOpenAiApiServiceMetrics metrics, TimeSpan? timeout = null)
    {
        return new OpenAiRequestContext(
            client,
            new Uri("https://api.example.com/llm"),
            new OpenAiProviderCredentials("secret"),
            new OpenAiRequestPolicy(timeout ?? TimeSpan.FromSeconds(30), true),
            NullLogger<OpenAiApiService>.Instance,
            metrics,
            new TraceMetadata("trace-id", "request-id"));
    }

    private static HttpResponseMessage HttpResponse(HttpStatusCode statusCode, string body)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send = send;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _send(request, cancellationToken);
    }

    private sealed class TestMetricsRecorder : IOpenAiApiServiceMetrics
    {
        public string? LastRequestStatus { get; private set; }
        public string? LastErrorClass { get; private set; }
        public TimeSpan LastLatency { get; private set; }
        public long BytesSent { get; private set; }
        public long BytesReceived { get; private set; }

        public void RecordRequest(string status, string errorClass)
        {
            LastRequestStatus = status;
            LastErrorClass = errorClass;
        }

        public void RecordLatency(TimeSpan duration) => LastLatency = duration;

        public void RecordBytesSent(long bytes) => BytesSent = bytes;

        public void RecordBytesReceived(long bytes) => BytesReceived += bytes;
    }
}
