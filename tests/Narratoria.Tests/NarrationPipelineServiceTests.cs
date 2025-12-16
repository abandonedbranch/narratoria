using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

        var observer = new RecordingObserver();
        var provider = new ProviderDispatchMiddleware(new StubNarrationProvider(["line-one", "line-two"]), observer: observer);
        var persistence = new NarrationPersistenceMiddleware(store, observer);
        var systemPrompt = new NarrationSystemPromptMiddleware(new StaticSystemPromptResolver(), observer);
        var guardian = new NarrationContentGuardianMiddleware(observer);
        var pipeline = new NarrationPipelineService(new NarrationMiddleware[] { persistence.InvokeAsync, systemPrompt.InvokeAsync, guardian.InvokeAsync, provider.InvokeAsync });

        var initialContext = new NarrationContext
        {
            SessionId = sessionId,
            PlayerPrompt = "player prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            Trace = new TraceMetadata("trace", "request")
        };

        var result = await pipeline.RunAsync(initialContext, CancellationToken.None);
        var received = new List<string>();
        await foreach (var token in result.StreamedNarration)
        {
            received.Add(token);
        }

        CollectionAssert.AreEqual(new[] { "line-one", "line-two" }, received);

        var persisted = store.Get(sessionId);
        CollectionAssert.AreEqual(new[] { "prior line", "line-one", "line-two" }, persisted.PriorNarration.ToArray());
        Assert.AreEqual(0, persisted.WorkingNarration.Length);
        Assert.AreEqual("player prompt", persisted.PlayerPrompt);

        CollectionAssert.AreEqual(new[] { "session_load", "system_prompt_injection", "content_guardian_injection", "provider_dispatch", "persist_context" }, observer.StageTelemetries.Select(t => t.Stage).ToArray());
        Assert.AreEqual(2, observer.StreamedTokens);
    }

    [TestMethod]
    public async Task SecondRun_ReinjectsPrompts_AndDoesNotPersistWorkingSegmentsOrFlags()
    {
        var sessionId = Guid.NewGuid();
        var store = InMemorySessionStore.WithSessions(new[] { CreateContext(sessionId) });

        var runOneObserver = new RecordingObserver();
        var runTwoObserver = new RecordingObserver();

        var providerOne = new ProviderDispatchMiddleware(new StubNarrationProvider(["one"], ["two"]), observer: runOneObserver);
        var providerTwo = new ProviderDispatchMiddleware(new StubNarrationProvider(["one"], ["two"]), observer: runTwoObserver);
        var systemPromptOne = new NarrationSystemPromptMiddleware(new StaticSystemPromptResolver(), runOneObserver);
        var systemPromptTwo = new NarrationSystemPromptMiddleware(new StaticSystemPromptResolver(), runTwoObserver);
        var guardianOne = new NarrationContentGuardianMiddleware(runOneObserver);
        var guardianTwo = new NarrationContentGuardianMiddleware(runTwoObserver);

        var pipelineOne = new NarrationPipelineService(new NarrationMiddleware[]
        {
            new NarrationPersistenceMiddleware(store, runOneObserver).InvokeAsync,
            systemPromptOne.InvokeAsync,
            guardianOne.InvokeAsync,
            providerOne.InvokeAsync
        });

        var pipelineTwo = new NarrationPipelineService(new NarrationMiddleware[]
        {
            new NarrationPersistenceMiddleware(store, runTwoObserver).InvokeAsync,
            systemPromptTwo.InvokeAsync,
            guardianTwo.InvokeAsync,
            providerTwo.InvokeAsync
        });

        var firstContext = new NarrationContext
        {
            SessionId = sessionId,
            PlayerPrompt = "prompt-1",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Trace = new TraceMetadata("trace-1", "req-1")
        };

        var firstResult = await pipelineOne.RunAsync(firstContext, CancellationToken.None);
        await ConsumeAsync(firstResult.StreamedNarration);

        var persistedAfterFirst = store.Get(sessionId);
        Assert.AreEqual(0, persistedAfterFirst.WorkingContextSegments.Length);
        Assert.IsFalse(persistedAfterFirst.Metadata.Keys.Any(k => k.StartsWith("system_prompt_", StringComparison.Ordinal)));
        Assert.IsFalse(persistedAfterFirst.Metadata.Keys.Any(k => k.StartsWith("content_guardian_", StringComparison.Ordinal)));

        var secondContext = new NarrationContext
        {
            SessionId = sessionId,
            PlayerPrompt = "prompt-2",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Trace = new TraceMetadata("trace-2", "req-2")
        };

        var secondResult = await pipelineTwo.RunAsync(secondContext, CancellationToken.None);
        await ConsumeAsync(secondResult.StreamedNarration);

        var secondStages = runTwoObserver.StageTelemetries.ToArray();
        var system = secondStages.Single(t => t.Stage == "system_prompt_injection");
        var guardianStage = secondStages.Single(t => t.Stage == "content_guardian_injection");
        Assert.AreEqual("success", system.Status);
        Assert.AreEqual("success", guardianStage.Status);
    }

    [TestMethod]
    public async Task MiddlewareOrder_RemainsDeterministic()
    {
        var sessionId = Guid.NewGuid();
        var store = InMemorySessionStore.WithSessions(new[] { CreateContext(sessionId) });
        var observer = new RecordingObserver();
        var order = new List<string>();

        ValueTask<MiddlewareResult> Recorder(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
        {
            order.Add("custom");
            return next(context, result, cancellationToken);
        }

        var provider = new ProviderDispatchMiddleware(new StubNarrationProvider(["a"]), observer: observer);
        var persistence = new NarrationPersistenceMiddleware(store, observer);
        var systemPrompt = new NarrationSystemPromptMiddleware(new StaticSystemPromptResolver(), observer);
        var guardian = new NarrationContentGuardianMiddleware(observer);
        var pipeline = new NarrationPipelineService(new NarrationMiddleware[] { persistence.InvokeAsync, systemPrompt.InvokeAsync, Recorder, guardian.InvokeAsync, provider.InvokeAsync });

        var context = new NarrationContext
        {
            SessionId = sessionId,
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Trace = new TraceMetadata("trace", "req")
        };

        var result = await pipeline.RunAsync(context, CancellationToken.None);
        await foreach (var _ in result.StreamedNarration)
        {
            // Consume the stream to trigger middleware execution
        }

        CollectionAssert.AreEqual(new[] { "custom" }, order);
        CollectionAssert.AreEqual(new[] { "session_load", "system_prompt_injection", "content_guardian_injection", "provider_dispatch", "persist_context" }, observer.StageTelemetries.Select(t => t.Stage).ToArray());
    }

    [TestMethod]
    public async Task ShortCircuitMiddleware_SkipsProviderAndPersist()
    {
        var sessionId = Guid.NewGuid();
        var store = InMemorySessionStore.WithSessions(new[] { CreateContext(sessionId) });
        var observer = new RecordingObserver();

        ValueTask<MiddlewareResult> ShortCircuit(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
        {
            var stream = AsyncEnumerable.Empty<string>();
            var updated = ValueTask.FromResult(context with { WorkingNarration = ImmutableArray<string>.Empty });
            return ValueTask.FromResult(new MiddlewareResult(stream, updated));
        }

        var provider = new ProviderDispatchMiddleware(new StubNarrationProvider(["unused"], onStream: () => Assert.Fail("Provider should not be invoked")), observer: observer);
        var persistence = new NarrationPersistenceMiddleware(store, observer);
        var systemPrompt = new NarrationSystemPromptMiddleware(new StaticSystemPromptResolver(), observer);
        var guardian = new NarrationContentGuardianMiddleware(observer);
        var pipeline = new NarrationPipelineService(new NarrationMiddleware[] { ShortCircuit, persistence.InvokeAsync, systemPrompt.InvokeAsync, guardian.InvokeAsync, provider.InvokeAsync });

        var context = new NarrationContext
        {
            SessionId = sessionId,
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Trace = new TraceMetadata("trace", "req")
        };

        var result = await pipeline.RunAsync(context, CancellationToken.None);
        var tokens = new List<string>();
        await foreach (var token in result.StreamedNarration)
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
        var observer = new RecordingObserver();
        using var cts = new CancellationTokenSource();

        var provider = new ProviderDispatchMiddleware(new StubNarrationProvider(StreamUntilCanceled), observer: observer, options: new ProviderDispatchOptions { Timeout = Timeout.InfiniteTimeSpan });
        var persistence = new NarrationPersistenceMiddleware(store, observer);
        var systemPrompt = new NarrationSystemPromptMiddleware(new StaticSystemPromptResolver(), observer);
        var guardian = new NarrationContentGuardianMiddleware(observer);
        var pipeline = new NarrationPipelineService(new NarrationMiddleware[] { persistence.InvokeAsync, systemPrompt.InvokeAsync, guardian.InvokeAsync, provider.InvokeAsync });

        var context = new NarrationContext
        {
            SessionId = sessionId,
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Trace = new TraceMetadata("trace", "req")
        };

        var tokens = new List<string>();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
        {
            var result = await pipeline.RunAsync(context, cts.Token);
            await foreach (var token in result.StreamedNarration.WithCancellation(cts.Token))
            {
                tokens.Add(token);
                cts.Cancel();
            }
        });

        CollectionAssert.AreEqual(new[] { "first" }, tokens);
        Assert.IsFalse(store.HasSaved);
        var stages = observer.StageTelemetries.Select(t => t.Stage).ToArray();
        CollectionAssert.AreEqual(new[] { "session_load", "system_prompt_injection", "content_guardian_injection", "provider_dispatch", "persist_context" }, stages, string.Join(",", stages));
        Assert.AreEqual("success", observer.StageTelemetries[1].Status);
        Assert.AreEqual("success", observer.StageTelemetries[2].Status);
        Assert.AreEqual("canceled", observer.StageTelemetries[3].Status);
        Assert.AreEqual("canceled", observer.StageTelemetries[4].Status);
    }

    [TestMethod]
    public async Task ProviderTimeout_StopsStreaming_AndSkipsPersistence()
    {
        var sessionId = Guid.NewGuid();
        var store = InMemorySessionStore.WithSessions(new[] { CreateContext(sessionId) });
        var observer = new RecordingObserver();

        var provider = new ProviderDispatchMiddleware(
            new StubNarrationProvider(DelayThenYield),
            observer: observer,
            options: new ProviderDispatchOptions { Timeout = TimeSpan.FromMilliseconds(25) });

        var pipeline = new NarrationPipelineService(new NarrationMiddleware[]
        {
            new NarrationPersistenceMiddleware(store, observer).InvokeAsync,
            new NarrationSystemPromptMiddleware(new StaticSystemPromptResolver(), observer).InvokeAsync,
            new NarrationContentGuardianMiddleware(observer).InvokeAsync,
            provider.InvokeAsync
        });

        var context = new NarrationContext
        {
            SessionId = sessionId,
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Trace = new TraceMetadata("trace", "req")
        };

        await Assert.ThrowsExceptionAsync<NarrationPipelineException>(async () =>
        {
            var result = await pipeline.RunAsync(context, CancellationToken.None);
            await ConsumeAsync(result.StreamedNarration);
        });

        Assert.IsFalse(store.HasSaved);

        var timeoutError = observer.Errors.Single(e => e.Stage == "provider_dispatch");
        Assert.AreEqual(NarrationPipelineErrorClass.ProviderTimeout, timeoutError.ErrorClass);

        var providerStage = observer.StageTelemetries.Single(t => t.Stage == "provider_dispatch");
        Assert.AreEqual("failure", providerStage.Status);
        Assert.AreEqual(NarrationPipelineErrorClass.ProviderTimeout.ToString(), providerStage.ErrorClass);

        var persistStage = observer.StageTelemetries.Single(t => t.Stage == "persist_context");
        Assert.AreEqual("skipped", persistStage.Status);
        Assert.AreEqual(NarrationPipelineErrorClass.ProviderTimeout.ToString(), persistStage.ErrorClass);
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

        var observer = new RecordingObserver();
        var provider = new ProviderDispatchMiddleware(new StubNarrationProvider(["one"], ["two"]), observer: observer);
        var persistence = new NarrationPersistenceMiddleware(store, observer);
        var systemPrompt = new NarrationSystemPromptMiddleware(new StaticSystemPromptResolver(), observer);
        var guardian = new NarrationContentGuardianMiddleware(observer);
        var pipeline = new NarrationPipelineService(new NarrationMiddleware[] { persistence.InvokeAsync, systemPrompt.InvokeAsync, guardian.InvokeAsync, provider.InvokeAsync });

        var firstContext = new NarrationContext
        {
            SessionId = firstSession,
            PlayerPrompt = "prompt-1",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Trace = new TraceMetadata("trace-1", "req-1")
        };

        var secondContext = new NarrationContext
        {
            SessionId = secondSession,
            PlayerPrompt = "prompt-2",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Trace = new TraceMetadata("trace-2", "req-2")
        };

        var firstRun = ConsumeAsync((await pipeline.RunAsync(firstContext, CancellationToken.None)).StreamedNarration);
        var secondRun = ConsumeAsync((await pipeline.RunAsync(secondContext, CancellationToken.None)).StreamedNarration);

        await Task.WhenAll(firstRun, secondRun);

        var firstPersisted = store.Get(firstSession);
        var secondPersisted = store.Get(secondSession);

        CollectionAssert.AreEqual(new[] { "one" }, firstPersisted.PriorNarration.ToArray());
        CollectionAssert.AreEqual(new[] { "two" }, secondPersisted.PriorNarration.ToArray());
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
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Trace = new TraceMetadata("trace", "request")
        };
    }

    private static async Task ConsumeAsync(IAsyncEnumerable<string> stream)
    {
        await foreach (var _ in stream)
        {
            // Consume all items from the stream
        }
    }

    private static async IAsyncEnumerable<string> StreamUntilCanceled([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return "first";
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(10, cancellationToken);
        }
    }

    private static async IAsyncEnumerable<string> DelayThenYield([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Delay(250, cancellationToken);
        yield return "late";
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

    private sealed class StaticSystemPromptResolver : ISystemPromptProfileResolver
    {
        private readonly SystemPromptProfile _profile = new("default", "You are the narrator.", ImmutableArray<string>.Empty, "v1");

        public ValueTask<SystemPromptProfile?> ResolveAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<SystemPromptProfile?>(_profile);
        }
    }
}
