using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Narration;
using Narratoria.OpenAi;

namespace Narratoria.Tests;

[TestClass]
public sealed class NarrationPipelineServiceTests
{
    [TestMethod]
    public async Task RunAsync_StreamsTokensAndPersistsContext()
    {
        var sessionId = Guid.NewGuid();
        var store = InMemorySessionStore.WithSessions(new[]
        {
            CreateContext(sessionId, "prior line")
        });
        var provider = new StubNarrationProvider(["line-one", "line-two"]);
        var observer = new RecordingObserver();
        var service = new NarrationPipelineService(store, provider, observer: observer);

        var request = new NarrationRequest(sessionId, "player prompt", new TraceMetadata("trace", "request"));
        var received = new List<string>();
        await foreach (var token in await service.RunAsync(request, CancellationToken.None))
        {
            received.Add(token);
        }

        CollectionAssert.AreEqual(new[] { "line-one", "line-two" }, received);

        var persisted = store.Get(sessionId);
        CollectionAssert.AreEqual(new[] { "prior line", "line-one", "line-two" }, persisted.PriorNarration.ToArray());
        Assert.AreEqual(0, persisted.WorkingNarration.Length);
        Assert.AreEqual("player prompt", persisted.PlayerPrompt);

        Assert.AreEqual(2, observer.StageTelemetries.Count);
        Assert.AreEqual("provider_dispatch", observer.StageTelemetries[0].Stage);
        Assert.AreEqual("persist_context", observer.StageTelemetries[1].Stage);
        Assert.AreEqual(2, observer.StreamedTokens);
    }

    [TestMethod]
    public async Task MiddlewareOrder_RemainsDeterministic()
    {
        var sessionId = Guid.NewGuid();
        var store = InMemorySessionStore.WithSessions(new[] { CreateContext(sessionId) });
        var provider = new StubNarrationProvider(["a"]);
        var observer = new RecordingObserver();
        var order = new List<string>();

        ValueTask<MiddlewareResult> Recorder(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
        {
            order.Add("custom");
            return next(context, result, cancellationToken);
        }

        var service = new NarrationPipelineService(store, provider, middleware: new NarrationMiddleware[] { Recorder }, observer: observer);
        var request = new NarrationRequest(sessionId, "prompt", new TraceMetadata("trace", "req"));
        await foreach (var _ in await service.RunAsync(request, CancellationToken.None))
        {
        }

        CollectionAssert.AreEqual(new[] { "custom" }, order);
        CollectionAssert.AreEqual(new[] { "provider_dispatch", "persist_context" }, observer.StageTelemetries.Select(t => t.Stage).ToArray());
    }

    [TestMethod]
    public async Task ShortCircuitMiddleware_SkipsProviderAndPersist()
    {
        var sessionId = Guid.NewGuid();
        var store = InMemorySessionStore.WithSessions(new[] { CreateContext(sessionId) });
        var provider = new StubNarrationProvider(["unused"], onStream: () => Assert.Fail("Provider should not be invoked"));
        var observer = new RecordingObserver();

        ValueTask<MiddlewareResult> ShortCircuit(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
        {
            var stream = AsyncEnumerable.Empty<string>();
            var updated = ValueTask.FromResult(context with { WorkingNarration = ImmutableArray<string>.Empty });
            return ValueTask.FromResult(new MiddlewareResult(stream, updated));
        }

        var service = new NarrationPipelineService(store, provider, middleware: new NarrationMiddleware[] { ShortCircuit }, observer: observer);
        var request = new NarrationRequest(sessionId, "prompt", new TraceMetadata("trace", "req"));

        var tokens = new List<string>();
        await foreach (var token in await service.RunAsync(request, CancellationToken.None))
        {
            tokens.Add(token);
        }

        Assert.AreEqual(0, tokens.Count);
        Assert.IsFalse(store.HasSaved);
        Assert.AreEqual(0, observer.StageTelemetries.Count);
    }

    [TestMethod]
    public async Task Cancellation_StopsStreamingAndDoesNotPersist()
    {
        var sessionId = Guid.NewGuid();
        var store = InMemorySessionStore.WithSessions(new[] { CreateContext(sessionId) });
        var provider = CancelAfter("first", "second");
        var observer = new RecordingObserver();
        var cts = new CancellationTokenSource();

        var service = new NarrationPipelineService(store, provider, observer: observer, options: new NarrationPipelineOptions { ProviderTimeout = Timeout.InfiniteTimeSpan });
        var request = new NarrationRequest(sessionId, "prompt", new TraceMetadata("trace", "req"));

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
        {
            await foreach (var token in (await service.RunAsync(request, cts.Token)).WithCancellation(cts.Token))
            {
                Assert.AreEqual("first", token);
                cts.Cancel();
            }
        });

        Assert.IsFalse(store.HasSaved);
        Assert.AreEqual("provider_dispatch", observer.StageTelemetries.Single().Stage);
    }

    [TestMethod]
    public async Task ConcurrentRuns_DoNotShareContext()
    {
        var firstSession = Guid.NewGuid();
        var secondSession = Guid.NewGuid();
        var store = InMemorySessionStore.WithSessions(new[]
        {
            CreateContext(firstSession),
            CreateContext(secondSession)
        });

        var provider = new StubNarrationProvider(["one"], ["two"]);
        var observer = new RecordingObserver();
        var service = new NarrationPipelineService(store, provider, observer: observer);

        var firstRequest = new NarrationRequest(firstSession, "prompt-1", new TraceMetadata("trace-1", "req-1"));
        var secondRequest = new NarrationRequest(secondSession, "prompt-2", new TraceMetadata("trace-2", "req-2"));

        var firstRun = ConsumeAsync(await service.RunAsync(firstRequest, CancellationToken.None));
        var secondRun = ConsumeAsync(await service.RunAsync(secondRequest, CancellationToken.None));

        await Task.WhenAll(firstRun, secondRun);

        var firstContext = store.Get(firstSession);
        var secondContext = store.Get(secondSession);

        CollectionAssert.AreEqual(new[] { "one" }, firstContext.PriorNarration.ToArray());
        CollectionAssert.AreEqual(new[] { "two" }, secondContext.PriorNarration.ToArray());
    }

    private static NarrationContext CreateContext(Guid sessionId, string? prior = null)
    {
        return new NarrationContext
        {
            SessionId = sessionId,
            PlayerPrompt = "prior prompt",
            PriorNarration = prior is null ? ImmutableArray<string>.Empty : ImmutableArray.Create(prior),
            WorkingNarration = ImmutableArray<string>.Empty,
            Metadata = ImmutableDictionary<string, string>.Empty,
            Trace = new TraceMetadata("trace", "request")
        };
    }

    private static StubNarrationProvider CancelAfter(params string[] tokens)
    {
        return new StubNarrationProvider(cancellationToken => CancelAfterCore(tokens, cancellationToken));
    }

    private static async IAsyncEnumerable<string> CancelAfterCore(IEnumerable<string> tokens, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var token in tokens)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return token;
            await Task.Yield();
        }
    }

    private static async Task ConsumeAsync(IAsyncEnumerable<string> stream)
    {
        await foreach (var _ in stream)
        {
        }
    }

    private sealed class StubNarrationProvider : INarrationProvider
    {
        private readonly Func<CancellationToken, IAsyncEnumerable<string>> _streamFactory;
        private readonly Action? _onStream;

        public StubNarrationProvider(IEnumerable<string> tokens, Action? onStream = null)
            : this(_ => Yield(tokens), onStream)
        {
        }

        public StubNarrationProvider(IEnumerable<string> firstRunTokens, IEnumerable<string> secondRunTokens)
            : this(CreateStream(firstRunTokens, secondRunTokens))
        {
        }

        public StubNarrationProvider(Func<CancellationToken, IAsyncEnumerable<string>> streamFactory, Action? onStream = null)
        {
            _streamFactory = streamFactory;
            _onStream = onStream;
        }

        public IAsyncEnumerable<string> StreamNarrationAsync(NarrationContext context, CancellationToken cancellationToken)
        {
            _onStream?.Invoke();
            return _streamFactory(cancellationToken);
        }

        private static Func<CancellationToken, IAsyncEnumerable<string>> CreateStream(IEnumerable<string> firstRun, IEnumerable<string> secondRun)
        {
            var queue = new Queue<IEnumerable<string>>();
            queue.Enqueue(firstRun);
            queue.Enqueue(secondRun);

            return cancellationToken =>
            {
                if (!queue.TryDequeue(out var next))
                {
                    return AsyncEnumerable.Empty<string>();
                }

                return Yield(next, cancellationToken);
            };
        }

        private static async IAsyncEnumerable<string> Yield(IEnumerable<string> tokens, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var token in tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return token;
                await Task.Yield();
            }
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

        public NarrationContext Get(Guid sessionId) => _sessions[sessionId];

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

    private sealed class RecordingObserver : INarrationPipelineObserver
    {
        private readonly List<NarrationStageTelemetry> _telemetries = new();

        public IReadOnlyList<NarrationStageTelemetry> StageTelemetries => _telemetries;

        public int StreamedTokens { get; private set; }

        public List<NarrationPipelineError> Errors { get; } = new();

        public void OnError(NarrationPipelineError error) => Errors.Add(error);

        public void OnStageCompleted(NarrationStageTelemetry telemetry) => _telemetries.Add(telemetry);

        public void OnTokensStreamed(Guid sessionId, int tokenCount) => StreamedTokens += tokenCount;
    }
}
