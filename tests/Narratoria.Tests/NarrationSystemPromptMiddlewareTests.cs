using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Narration;
using Narratoria.OpenAi;

namespace Narratoria.Tests;

[TestClass]
public sealed class NarrationSystemPromptMiddlewareTests
{
    [TestMethod]
    public async Task InsertsSystemPromptAndInstructionsBeforeExistingSegments()
    {
        var resolver = new FakeSystemPromptProfileResolver(new SystemPromptProfile(
            ProfileId: "test-profile",
            PromptText: "Be a helpful narrator.",
            Instructions: ImmutableArray.Create("Rule 1", "Rule 2"),
            Version: "v1"
        ));
        var observer = new RecordingObserver();
        var middleware = new NarrationSystemPromptMiddleware(resolver, observer);

        var existingSegments = ImmutableArray.Create(
            new ContextSegment("user", "player input", "player_prompt")
        );

        var context = new NarrationContext
        {
            SessionId = Guid.NewGuid(),
            PlayerPrompt = "Continue the story.",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = existingSegments,
            Trace = new TraceMetadata("trace", "request")
        };

        NarrationContext? received = null;
        var result = MiddlewareResult.FromContext(context);
        await middleware.InvokeAsync(context, result, CaptureContext, CancellationToken.None);

        Assert.IsNotNull(received);
        Assert.AreEqual(4, received!.WorkingContextSegments.Length);
        Assert.AreEqual("system", received.WorkingContextSegments[0].Role);
        Assert.AreEqual("Be a helpful narrator.", received.WorkingContextSegments[0].Content);
        Assert.AreEqual("instruction", received.WorkingContextSegments[1].Role);
        Assert.AreEqual("Rule 1", received.WorkingContextSegments[1].Content);
        Assert.AreEqual("instruction", received.WorkingContextSegments[2].Role);
        Assert.AreEqual("Rule 2", received.WorkingContextSegments[2].Content);
        Assert.AreEqual("user", received.WorkingContextSegments[3].Role);
        Assert.AreEqual("player input", received.WorkingContextSegments[3].Content);
        Assert.AreEqual("success", observer.StageTelemetries.Single().Status);

        ValueTask<MiddlewareResult> CaptureContext(NarrationContext updated, MiddlewareResult downstream, CancellationToken ct)
        {
            received = updated;
            return ValueTask.FromResult(downstream);
        }
    }

    [TestMethod]
    public async Task UpdatesMetadataWithProfileIdAndVersion()
    {
        var resolver = new FakeSystemPromptProfileResolver(new SystemPromptProfile(
            ProfileId: "profile-123",
            PromptText: "System prompt",
            Instructions: [],
            Version: "v2"
        ));
        var middleware = new NarrationSystemPromptMiddleware(resolver);

        var context = new NarrationContext
        {
            SessionId = Guid.NewGuid(),
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = [],
            Trace = new TraceMetadata("trace", "request")
        };

        NarrationContext? received = null;
        var result = MiddlewareResult.FromContext(context);
        await middleware.InvokeAsync(context, result, CaptureContext, CancellationToken.None);

        Assert.IsNotNull(received);
        Assert.AreEqual("profile-123", received!.Metadata["system_prompt_profile_id"]);
        Assert.AreEqual("v2", received.Metadata["system_prompt_version"]);

        ValueTask<MiddlewareResult> CaptureContext(NarrationContext updated, MiddlewareResult downstream, CancellationToken ct)
        {
            received = updated;
            return ValueTask.FromResult(downstream);
        }
    }

    [TestMethod]
    public async Task SkipsReinsertion_WhenSameProfileAndVersionAlreadyApplied()
    {
        var resolver = new FakeSystemPromptProfileResolver(new SystemPromptProfile(
            ProfileId: "profile-x",
            PromptText: "New prompt",
            Instructions: ImmutableArray.Create("New rule"),
            Version: "v1"
        ));
        var observer = new RecordingObserver();
        var middleware = new NarrationSystemPromptMiddleware(resolver, observer);

        var metadata = ImmutableDictionary<string, string>.Empty
            .Add("system_prompt_profile_id", "profile-x")
            .Add("system_prompt_version", "v1");

        var existingSegments = ImmutableArray.Create(
            new ContextSegment("system", "Old prompt", "system_prompt_middleware"),
            new ContextSegment("instruction", "Old rule", "system_prompt_middleware"),
            new ContextSegment("user", "player", "player_prompt")
        );

        var context = new NarrationContext
        {
            SessionId = Guid.NewGuid(),
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = metadata,
            WorkingContextSegments = existingSegments,
            Trace = new TraceMetadata("trace", "request")
        };

        var result = MiddlewareResult.FromContext(context);
        var downstream = await middleware.InvokeAsync(context, result, (ctx, res, ct) => ValueTask.FromResult(res), CancellationToken.None);

        Assert.AreSame(result.StreamedNarration, downstream.StreamedNarration);
        Assert.AreEqual("skipped", observer.StageTelemetries.Single().Status);
        var updatedContext = await downstream.UpdatedContext;
        Assert.AreEqual(3, updatedContext.WorkingContextSegments.Length);
    }

    [TestMethod]
    [DataRow(null, "profile unavailable")]
    [DataRow("", "empty prompt")]
    [DataRow("   ", "whitespace prompt")]
    public async Task ThrowsNarrationPipelineException_WhenPromptUnavailableOrEmpty(string? promptText, string _)
    {
        var resolver = promptText == null
            ? new FakeSystemPromptProfileResolver(null)
            : new FakeSystemPromptProfileResolver(new SystemPromptProfile(
                ProfileId: "test",
                PromptText: promptText,
                Instructions: [],
                Version: "v1"
            ));

        var observer = new RecordingObserver();
        var middleware = new NarrationSystemPromptMiddleware(resolver, observer);

        var context = new NarrationContext
        {
            SessionId = Guid.NewGuid(),
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = [],
            Trace = new TraceMetadata("trace", "request")
        };

        var result = MiddlewareResult.FromContext(context);

        var ex = await Assert.ThrowsExceptionAsync<NarrationPipelineException>(
            () => middleware.InvokeAsync(context, result, (ctx, res, ct) => ValueTask.FromResult(res), CancellationToken.None).AsTask()
        );

        Assert.AreEqual("System prompt profile unavailable or prompt text is empty", ex.Error.Message);
        Assert.AreEqual("system_prompt_injection", ex.Error.Stage);
        Assert.AreEqual(NarrationPipelineErrorClass.ContextError, ex.Error.ErrorClass);
        Assert.AreEqual("failure", observer.StageTelemetries.Single().Status);
    }

    [TestMethod]
    public async Task PropagatesCancellation_WithoutInvokingDownstream()
    {
        var resolver = new FakeSystemPromptProfileResolver(new SystemPromptProfile(
            ProfileId: "test",
            PromptText: "prompt",
            Instructions: [],
            Version: "v1"
        ));

        var observer = new RecordingObserver();
        var middleware = new NarrationSystemPromptMiddleware(resolver, observer);

        var context = new NarrationContext
        {
            SessionId = Guid.NewGuid(),
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = [],
            Trace = new TraceMetadata("trace", "request")
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = MiddlewareResult.FromContext(context);
        var downstreamInvoked = false;

        var ex = await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => middleware.InvokeAsync(context, result, (ctx, res, ct) =>
            {
                downstreamInvoked = true;
                return ValueTask.FromResult(res);
            }, cts.Token).AsTask()
        );

        Assert.IsFalse(downstreamInvoked);
    }

    [TestMethod]
    public async Task FiltersOutEmptyInstructions()
    {
        var resolver = new FakeSystemPromptProfileResolver(new SystemPromptProfile(
            ProfileId: "test",
            PromptText: "System prompt",
            Instructions: ImmutableArray.Create("Rule 1", "", "   ", "Rule 2"),
            Version: "v1"
        ));
        var middleware = new NarrationSystemPromptMiddleware(resolver);

        var context = new NarrationContext
        {
            SessionId = Guid.NewGuid(),
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = [],
            Trace = new TraceMetadata("trace", "request")
        };

        NarrationContext? received = null;
        var result = MiddlewareResult.FromContext(context);
        await middleware.InvokeAsync(context, result, CaptureContext, CancellationToken.None);

        Assert.IsNotNull(received);
        Assert.AreEqual(3, received!.WorkingContextSegments.Length);
        Assert.AreEqual("system", received.WorkingContextSegments[0].Role);
        Assert.AreEqual("instruction", received.WorkingContextSegments[1].Role);
        Assert.AreEqual("Rule 1", received.WorkingContextSegments[1].Content);
        Assert.AreEqual("instruction", received.WorkingContextSegments[2].Role);
        Assert.AreEqual("Rule 2", received.WorkingContextSegments[2].Content);

        ValueTask<MiddlewareResult> CaptureContext(NarrationContext updated, MiddlewareResult downstream, CancellationToken ct)
        {
            received = updated;
            return ValueTask.FromResult(downstream);
        }
    }

    [TestMethod]
    public async Task PreservesExistingSegmentOrder()
    {
        var resolver = new FakeSystemPromptProfileResolver(new SystemPromptProfile(
            ProfileId: "test",
            PromptText: "System",
            Instructions: ImmutableArray.Create("Instruction"),
            Version: "v1"
        ));
        var middleware = new NarrationSystemPromptMiddleware(resolver);

        var existingSegments = ImmutableArray.Create(
            new ContextSegment("history", "prior line 1", "prior_narration"),
            new ContextSegment("history", "prior line 2", "prior_narration"),
            new ContextSegment("user", "player prompt", "player_prompt")
        );

        var context = new NarrationContext
        {
            SessionId = Guid.NewGuid(),
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = ImmutableDictionary<string, string>.Empty,
            WorkingContextSegments = existingSegments,
            Trace = new TraceMetadata("trace", "request")
        };

        NarrationContext? received = null;
        var result = MiddlewareResult.FromContext(context);
        await middleware.InvokeAsync(context, result, CaptureContext, CancellationToken.None);

        Assert.IsNotNull(received);
        Assert.AreEqual(5, received!.WorkingContextSegments.Length);
        Assert.AreEqual("system", received.WorkingContextSegments[0].Role);
        Assert.AreEqual("instruction", received.WorkingContextSegments[1].Role);
        Assert.AreEqual("history", received.WorkingContextSegments[2].Role);
        Assert.AreEqual("prior line 1", received.WorkingContextSegments[2].Content);
        Assert.AreEqual("history", received.WorkingContextSegments[3].Role);
        Assert.AreEqual("prior line 2", received.WorkingContextSegments[3].Content);
        Assert.AreEqual("user", received.WorkingContextSegments[4].Role);

        ValueTask<MiddlewareResult> CaptureContext(NarrationContext updated, MiddlewareResult downstream, CancellationToken ct)
        {
            received = updated;
            return ValueTask.FromResult(downstream);
        }
    }

    [TestMethod]
    public async Task AllowsProfileUpdateByDifferentVersionOrId()
    {
        var profile1 = new SystemPromptProfile(
            ProfileId: "profile-a",
            PromptText: "First prompt",
            Instructions: [],
            Version: "v1"
        );

        var profile2 = new SystemPromptProfile(
            ProfileId: "profile-b",
            PromptText: "Second prompt",
            Instructions: [],
            Version: "v1"
        );

        var resolver = new FakeSystemPromptProfileResolver(profile2);

        var metadata = ImmutableDictionary<string, string>.Empty
            .Add("system_prompt_profile_id", "profile-a")
            .Add("system_prompt_version", "v1");

        var middleware = new NarrationSystemPromptMiddleware(resolver);

        var context = new NarrationContext
        {
            SessionId = Guid.NewGuid(),
            PlayerPrompt = "prompt",
            PriorNarration = [],
            WorkingNarration = [],
            Metadata = metadata,
            WorkingContextSegments = [],
            Trace = new TraceMetadata("trace", "request")
        };

        NarrationContext? received = null;
        var result = MiddlewareResult.FromContext(context);
        await middleware.InvokeAsync(context, result, CaptureContext, CancellationToken.None);

        Assert.IsNotNull(received);
        Assert.AreEqual("profile-b", received!.Metadata["system_prompt_profile_id"]);
        Assert.AreEqual("Second prompt", received.WorkingContextSegments[0].Content);

        ValueTask<MiddlewareResult> CaptureContext(NarrationContext updated, MiddlewareResult downstream, CancellationToken ct)
        {
            received = updated;
            return ValueTask.FromResult(downstream);
        }
    }

    private sealed class FakeSystemPromptProfileResolver : ISystemPromptProfileResolver
    {
        private readonly SystemPromptProfile? _profile;

        public FakeSystemPromptProfileResolver(SystemPromptProfile? profile)
        {
            _profile = profile;
        }

        public ValueTask<SystemPromptProfile?> ResolveAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_profile);
        }
    }

    private sealed class RecordingObserver : INarrationPipelineObserver
    {
        private readonly List<NarrationStageTelemetry> _telemetries = new();
        private readonly List<NarrationPipelineError> _errors = new();

        public IReadOnlyList<NarrationStageTelemetry> StageTelemetries => _telemetries;
        public IReadOnlyList<NarrationPipelineError> Errors => _errors;

        public void OnError(NarrationPipelineError error) => _errors.Add(error);

        public void OnStageCompleted(NarrationStageTelemetry telemetry) => _telemetries.Add(telemetry);

        public void OnTokensStreamed(Guid sessionId, int tokenCount)
        {
        }
    }
}
