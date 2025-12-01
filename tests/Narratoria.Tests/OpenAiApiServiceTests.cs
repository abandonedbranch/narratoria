using System.Text.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.OpenAi;
using OpenAI.Chat;

namespace Narratoria.Tests;

#pragma warning disable OPENAI001

[TestClass]
public sealed class OpenAiApiServiceTests
{
    [TestMethod]
    public async Task StreamAsync_YieldsProviderTokens()
    {
        var metrics = new TestMetricsRecorder();
        var provider = new TestStreamingProvider((_, ct) => StreamUpdates(new[]
            {
                OpenAIChatModelFactory.StreamingChatCompletionUpdate(contentUpdate: new ChatMessageContent("hello")),
                OpenAIChatModelFactory.StreamingChatCompletionUpdate(contentUpdate: new ChatMessageContent("world"), finishReason: ChatFinishReason.Stop)
        }));

        var context = CreateContext(provider, metrics);
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
        var metrics = new TestMetricsRecorder();
        var provider = new TestStreamingProvider((_, ct) => Throw(new HttpRequestException("boom"), ct));
        var context = CreateContext(provider, metrics);
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
        var metrics = new TestMetricsRecorder();
        var provider = new TestStreamingProvider((_, ct) => TimeoutStream(ct));
        var context = CreateContext(provider, metrics, timeout: TimeSpan.FromMilliseconds(10));
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
        var metrics = new TestMetricsRecorder();
        var provider = new TestStreamingProvider((_, ct) => Throw(new JsonException("boom"), ct));
        var context = CreateContext(provider, metrics);
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

    private static async IAsyncEnumerable<StreamingChatCompletionUpdate> StreamUpdates(
        IEnumerable<StreamingChatCompletionUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<StreamingChatCompletionUpdate> TimeoutStream([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            yield return OpenAIChatModelFactory.StreamingChatCompletionUpdate(contentUpdate: new ChatMessageContent(string.Empty));
        }
    }

    private static async IAsyncEnumerable<StreamingChatCompletionUpdate> Throw(Exception exception, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        if (cancellationToken.IsCancellationRequested)
        {
            yield break;
        }

        throw exception;
    }

    private static OpenAiRequestContext CreateContext(IOpenAiStreamingProvider provider, IOpenAiApiServiceMetrics metrics, TimeSpan? timeout = null)
    {
        return new OpenAiRequestContext(
            new HttpClient(),
            new Uri("https://api.example.com/llm"),
            new OpenAiProviderCredentials("secret"),
            new OpenAiRequestPolicy(timeout ?? TimeSpan.FromSeconds(30), true),
            NullLogger<OpenAiApiService>.Instance,
            metrics,
            new TraceMetadata("trace-id", "request-id"),
            provider);
    }

    private sealed class TestStreamingProvider : IOpenAiStreamingProvider
    {
        private readonly Func<SerializedPrompt, CancellationToken, IAsyncEnumerable<StreamingChatCompletionUpdate>> _stream;

        public TestStreamingProvider(Func<SerializedPrompt, CancellationToken, IAsyncEnumerable<StreamingChatCompletionUpdate>> stream)
        {
            _stream = stream;
        }

        public IAsyncEnumerable<StreamingChatCompletionUpdate> StreamAsync(SerializedPrompt prompt, CancellationToken cancellationToken)
            => _stream(prompt, cancellationToken);
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
#pragma warning restore OPENAI001

}
