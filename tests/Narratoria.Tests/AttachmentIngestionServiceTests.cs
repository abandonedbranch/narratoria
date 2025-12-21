using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Narration;
using Narratoria.Narration.Attachments;
using Narratoria.OpenAi;
using Narratoria.Storage;

namespace Narratoria.Tests;

[TestClass]
public sealed class AttachmentIngestionServiceTests
{
    private static readonly HttpClient SharedHttpClient = new();

    [TestMethod]
    public async Task IngestAsync_Succeeds_PersistsAndPurges()
    {
        var sessionId = Guid.NewGuid();
        var upload = new UploadedFile(sessionId, "att-1", "notes.md", "text/markdown", 12, Utf8("content"));
        var uploads = InMemoryUploadStore.WithUploads(sessionId, upload);
        var processed = new InMemoryProcessedAttachmentStore();
        var openAi = new StubOpenAiApiService(["summary "]);
        var contextFactory = new StaticAttachmentOpenAiContextFactory(CreateContext());
        var service = new AttachmentIngestionService(uploads, processed, openAi, contextFactory, NullLogger<AttachmentIngestionService>.Instance);

        var command = new AttachmentIngestionCommand(sessionId, upload.AttachmentId, new TraceMetadata("trace", "request"), AttachmentIngestionOptions.Default);
        var result = await service.IngestAsync(command, CancellationToken.None);

        Assert.IsTrue(result.Ok);
        Assert.AreEqual(0, uploads.Count(sessionId));
        Assert.AreEqual(1, processed.Attachments.Count);
        Assert.AreEqual("att-1", processed.Attachments.Single().AttachmentId);
    }

    [TestMethod]
    public async Task IngestAsync_FileTooLarge_FailsAndPurges()
    {
        var sessionId = Guid.NewGuid();
        var upload = new UploadedFile(sessionId, "att-2", "notes.md", "text/markdown", 1024, Utf8("content"));
        var uploads = InMemoryUploadStore.WithUploads(sessionId, upload);
        var processed = new InMemoryProcessedAttachmentStore();
        var openAi = new StubOpenAiApiService(["ignored"]);
        var contextFactory = new StaticAttachmentOpenAiContextFactory(CreateContext());
        var options = AttachmentIngestionOptions.Default with { MaxBytes = 10 };
        var service = new AttachmentIngestionService(uploads, processed, openAi, contextFactory, NullLogger<AttachmentIngestionService>.Instance);

        var command = new AttachmentIngestionCommand(sessionId, upload.AttachmentId, new TraceMetadata("trace", "request"), options);
        var result = await service.IngestAsync(command, CancellationToken.None);

        Assert.IsFalse(result.Ok);
        Assert.AreEqual("FileTooLarge", result.Error?.ErrorClass);
        Assert.AreEqual(0, uploads.Count(sessionId));
        Assert.AreEqual(0, processed.Attachments.Count);
    }

    [TestMethod]
    public async Task Middleware_ShortCircuitsOnFailure()
    {
        var sessionId = Guid.NewGuid();
        var store = InMemorySessionStore.WithSessions(new[]
        {
            new NarrationContext
            {
                SessionId = sessionId,
                PlayerPrompt = "prompt",
                PriorNarration = [],
                WorkingNarration = [],
                Metadata = ImmutableDictionary<string, string>.Empty,
                Trace = new TraceMetadata("trace", "request")
            }
        });

        var provider = new ProviderDispatchMiddleware(new ThrowingProvider());
        var failingService = new FailingIngestionService();
        var middleware = new AttachmentIngestionMiddleware("missing", failingService);
        var persistence = new NarrationPersistenceMiddleware(store);
        var pipeline = new NarrationPipelineService(new NarrationMiddleware[] { persistence.InvokeAsync, middleware.InvokeAsync, provider.InvokeAsync });
        var context = new NarrationContext
        {
            SessionId = sessionId,
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            Trace = new TraceMetadata("trace", "request")
        };

        await Assert.ThrowsExceptionAsync<NarrationPipelineException>(async () =>
        {
            var result = await pipeline.RunAsync(context, CancellationToken.None);
            await foreach (var _ in result.StreamedNarration)
            {
                // Consume the stream to trigger the pipeline execution and verify the exception is thrown
            }
        });

        Assert.IsFalse(store.HasSaved);
    }

    private static byte[] Utf8(string text) => System.Text.Encoding.UTF8.GetBytes(text);

    private static OpenAiRequestContext CreateContext()
    {
        var credentials = new OpenAiProviderCredentials("key");
        return new OpenAiRequestContext(SharedHttpClient, new Uri("https://example.com"), credentials, OpenAiRequestPolicy.Default, NullLogger.Instance, new NullOpenAiMetrics(), new TraceMetadata("trace", "req"), new DummyStreamingProvider());
    }

    private sealed class InMemoryUploadStore : IAttachmentUploadStore
    {
        private readonly ConcurrentDictionary<(Guid, string), UploadedFile> _uploads;

        private InMemoryUploadStore(IEnumerable<UploadedFile> uploads)
        {
            _uploads = new ConcurrentDictionary<(Guid, string), UploadedFile>(uploads.ToDictionary(x => (x.SessionId, x.AttachmentId), x => x));
        }

        public static InMemoryUploadStore WithUploads(Guid sessionId, params UploadedFile[] uploads) =>
            new(uploads.Select(u => u with { SessionId = sessionId }));

        public int Count(Guid sessionId) => _uploads.Keys.Count(k => k.Item1 == sessionId);

        public async ValueTask<string> WriteAsync(Guid sessionId, string fileName, string mimeType, long sizeBytes, Stream content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(content);

            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();

            var id = Guid.NewGuid().ToString("N");
            var upload = new UploadedFile(sessionId, id, fileName, mimeType, sizeBytes, bytes);
            _uploads[(sessionId, id)] = upload;
            return id;
        }

        public ValueTask DeleteAsync(Guid sessionId, string attachmentId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _uploads.TryRemove((sessionId, attachmentId), out _);
            return ValueTask.CompletedTask;
        }

        public ValueTask<UploadedFile?> GetAsync(Guid sessionId, string attachmentId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _uploads.TryGetValue((sessionId, attachmentId), out var file);
            return ValueTask.FromResult<UploadedFile?>(file);
        }
    }

    private sealed class InMemoryProcessedAttachmentStore : IProcessedAttachmentStore
    {
        private readonly ConcurrentDictionary<string, ProcessedAttachment> _attachments = new();

        public IReadOnlyCollection<ProcessedAttachment> Attachments => _attachments.Values.ToArray();

        public ValueTask<StorageResult<ProcessedAttachment?>> GetAsync(Guid sessionId, string attachmentId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_attachments.TryGetValue(attachmentId, out var found) && found.SessionId == sessionId)
            {
                return ValueTask.FromResult(StorageResult<ProcessedAttachment?>.Success(found));
            }

            return ValueTask.FromResult(StorageResult<ProcessedAttachment?>.Success(null));
        }

        public ValueTask<StorageResult<IReadOnlyList<ProcessedAttachment>>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matches = _attachments.Values
                .Where(a => a.SessionId == sessionId)
                .OrderBy(a => a.CreatedAt)
                .ToArray();
            return ValueTask.FromResult(StorageResult<IReadOnlyList<ProcessedAttachment>>.Success(matches));
        }

        public ValueTask<StorageResult<ProcessedAttachment?>> FindByHashAsync(Guid sessionId, string sourceHash, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var match = _attachments.Values.FirstOrDefault(a => a.SessionId == sessionId && a.SourceHash == sourceHash);
            return ValueTask.FromResult(StorageResult<ProcessedAttachment?>.Success(match));
        }

        public ValueTask<StorageResult<Unit>> SaveAsync(ProcessedAttachment attachment, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _attachments[attachment.AttachmentId] = attachment;
            return ValueTask.FromResult(StorageResult<Unit>.Success(Unit.Value));
        }

        public ValueTask<StorageResult<Unit>> DeleteAsync(Guid sessionId, string attachmentId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_attachments.TryGetValue(attachmentId, out var existing) && existing.SessionId == sessionId)
            {
                _attachments.TryRemove(attachmentId, out _);
            }

            return ValueTask.FromResult(StorageResult<Unit>.Success(Unit.Value));
        }
    }

    private sealed class StubOpenAiApiService : IOpenAiApiService
    {
        private readonly IReadOnlyList<string> _tokens;

        public StubOpenAiApiService(IReadOnlyList<string> tokens)
        {
            _tokens = tokens;
        }

        public async IAsyncEnumerable<StreamedToken> StreamAsync(SerializedPrompt prompt, OpenAiRequestContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var token in _tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new StreamedToken(token, false, DateTimeOffset.UtcNow);
                await Task.Yield();
            }
        }
    }

    private sealed class StaticAttachmentOpenAiContextFactory : IAttachmentOpenAiContextFactory
    {
        private readonly OpenAiRequestContext _context;

        public StaticAttachmentOpenAiContextFactory(OpenAiRequestContext context)
        {
            _context = context;
        }

        public OpenAiRequestContext Create(TraceMetadata trace)
        {
            return _context with { Trace = trace };
        }
    }

    private sealed class DummyStreamingProvider : IOpenAiStreamingProvider
    {
        public IAsyncEnumerable<OpenAI.Chat.StreamingChatCompletionUpdate> StreamAsync(SerializedPrompt prompt, CancellationToken cancellationToken)
        {
            return AsyncEnumerable.Empty<OpenAI.Chat.StreamingChatCompletionUpdate>();
        }
    }

    private sealed class NullOpenAiMetrics : IOpenAiApiServiceMetrics
    {
        public void RecordBytesReceived(long bytes)
        {
        }

        public void RecordBytesSent(long bytes)
        {
        }

        public void RecordLatency(TimeSpan duration)
        {
        }

        public void RecordRequest(string status, string errorClass)
        {
        }
    }

    private sealed class FailingIngestionService : IAttachmentIngestionService
    {
        public ValueTask<AttachmentIngestionResult> IngestAsync(AttachmentIngestionCommand command, CancellationToken cancellationToken)
        {
            var error = new AttachmentIngestionError("UnsupportedFileType", "Unsupported attachment.");
            return ValueTask.FromResult(AttachmentIngestionResult.Failure(error));
        }
    }

    private sealed class ThrowingProvider : INarrationProvider
    {
        public IAsyncEnumerable<string> StreamNarrationAsync(NarrationContext context, CancellationToken cancellationToken)
        {
            Assert.Fail("Provider should not be invoked when ingestion fails.");
            return AsyncEnumerable.Empty<string>();
        }
    }

    private sealed class InMemorySessionStore : INarrationSessionStore
    {
        private readonly ConcurrentDictionary<Guid, NarrationContext> _sessions;
        private bool _hasSaved;

        private InMemorySessionStore(IEnumerable<NarrationContext> sessions)
        {
            _sessions = new ConcurrentDictionary<Guid, NarrationContext>(sessions.ToDictionary(x => x.SessionId, x => x));
        }

        public bool HasSaved => _hasSaved;

        public static InMemorySessionStore WithSessions(IEnumerable<NarrationContext> sessions) => new(sessions);

        public ValueTask<NarrationContext?> LoadAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessions.TryGetValue(sessionId, out var context);
            return ValueTask.FromResult<NarrationContext?>(context);
        }

        public ValueTask SaveAsync(NarrationContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessions[context.SessionId] = context;
            _hasSaved = true;
            return ValueTask.CompletedTask;
        }
    }
}
